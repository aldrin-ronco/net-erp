# API Requirement — Copy Price List & Copy Promotion

## Contexto

El cliente .NET ERP permite al usuario duplicar listas de precios y promociones. Hoy el flujo es client-side:

1. `createPriceList(input)` — crea la lista/promo destino.
2. Paginar `priceListItemCatalogPage(priceListId: source)` para obtener todos los `PriceListDetail` del origen.
3. Llamar `batchUpdatePriceListPrices(input)` con los details en bloques.

**Problemas:**
- **N roundtrips**: una página de items por bloque + mutación por batch; escala mal con listas grandes.
- **No atómico**: si el batch falla a medio camino queda una lista huérfana con datos parciales.
- **Duplicación de lógica**: el cliente tiene que replicar todos los atributos heredados manualmente.

**Objetivo:** exponer dos mutaciones server-side que copien metadatos + todos los `PriceListDetail` de forma atómica en una sola llamada.

## Alcance

| Mutation | Source | Resultado |
|---|---|---|
| `copyPriceList` | Lista base (`parent == null`) | Nueva lista base |
| `copyPromotion` | Promoción (`parent != null`) | Nueva promoción bajo el mismo parent (o uno distinto si se provee `parentId`) |

**Fuera de scope:** crear promoción derivada de una lista base (`createPromotionFromPriceList`), copia cross-company, copia parcial de details. Estos pueden agregarse en iteraciones posteriores.

## Convenciones del schema (a respetar)

- Mutations: `camelCase` verb-first — `createPriceList`, `batchUpdatePriceListPrices`.
- Inputs: `PascalCase` + sufijo `Input` — `CreatePriceListInput`.
- Payloads: `PascalCase` + sufijo `Payload` con shape `{ entity, success: Boolean!, message: String, errors: [FieldError] }`.
- Enums: `SCREAMING_SNAKE_CASE`.
- IDs: `ID!` (nunca `Int!`).
- Fechas: `IsoDatetime` (scalar custom con offset Colombia -05:00).

## Schema a agregar

```graphql
# ============================================================
# Copy Price List (base list → base list)
# ============================================================

"""
Copy a base price list (parent == null) including all its price list details.
The new price list is created as a base list (no parent). Inherits all
attributes from the source unless overridden in the input.
"""
input CopyPriceListInput {
  "ID of the source price list to copy from. Must be a base list (parent == null)."
  sourcePriceListId: ID!

  "Name for the new price list. Must be unique across all price lists (archived or not) within the company."
  name: String!

  """
  Whether to copy all PriceListDetail rows from the source. Defaults to true.
  When false, the new list is created empty and the caller can populate via
  batchUpdatePriceListPrices.
  """
  copyDetails: Boolean

  # --- Optional overrides (null = inherit from source) ---
  editablePrice: Boolean
  isActive: Boolean
  autoApplyDiscount: Boolean
  isPublic: Boolean
  allowNewUsersAccess: Boolean
  listUpdateBehaviorOnCostChange: String
  costMode: PriceListCostMode
  isTaxable: Boolean
  priceListIncludeTax: Boolean
  useAlternativeFormula: Boolean
  storageId: ID

  """
  Payment method IDs to exclude. Null = inherit from source (full copy).
  When provided (including []) = full replace, same semantics as CreatePriceListInput.
  """
  excludedPaymentMethodIds: [ID]
}

"Response for copyPriceList mutation"
type CopyPriceListPayload {
  "The newly created price list (null on failure)"
  priceList: PriceList

  "Number of PriceListDetail rows copied from source"
  copiedDetailsCount: Int!

  success: Boolean!
  message: String
  errors: [FieldError]
}


# ============================================================
# Copy Promotion (promotion → promotion)
# ============================================================

"""
Copy a promotion (price list with parent != null) including all its price
list details. Structural attributes (isTaxable, priceListIncludeTax,
useAlternativeFormula, costMode, storageId, excludedPaymentMethods,
editablePrice, listUpdateBehaviorOnCostChange) are inherited from the
parent price list, not from the source promotion.
"""
input CopyPromotionInput {
  "ID of the source promotion to copy from. Must have parent != null."
  sourcePromotionId: ID!

  "Name for the new promotion. Must be unique across all price lists (archived or not) within the company."
  name: String!

  """
  Parent price list ID under which the new promotion will live.
  Null = inherit the parent from the source promotion.
  If provided, must refer to a non-archived base price list (parent == null).
  """
  parentId: ID

  "Start date of the new promotion. Null = inherit from source."
  startDate: IsoDatetime

  "End date of the new promotion. Null = inherit from source."
  endDate: IsoDatetime

  "Whether to copy all PriceListDetail rows from the source. Defaults to true."
  copyDetails: Boolean

  # --- Optional overrides (null = inherit from source) ---
  isActive: Boolean
  autoApplyDiscount: Boolean
  isPublic: Boolean
  allowNewUsersAccess: Boolean
}

"Response for copyPromotion mutation"
type CopyPromotionPayload {
  "The newly created promotion (null on failure)"
  priceList: PriceList

  "Number of PriceListDetail rows copied from source"
  copiedDetailsCount: Int!

  success: Boolean!
  message: String
  errors: [FieldError]
}


# ============================================================
# Root Mutation additions
# ============================================================

extend type RootMutationType {
  "Copy a base price list, duplicating its details atomically"
  copyPriceList(input: CopyPriceListInput!): CopyPriceListPayload

  "Copy a promotion, duplicating its details atomically"
  copyPromotion(input: CopyPromotionInput!): CopyPromotionPayload
}
```

## Comportamiento — `copyPriceList`

1. **Validar permiso**: el usuario actual debe tener `price_list.copy`.
2. **Validar `sourcePriceListId`**: debe existir, no estar archivada, y tener `parent_id IS NULL`.
3. **Validar `name`**: único entre **todas** las price_lists de la empresa (archivadas o no, base o promo).
4. **Validar `storageId`** (si se provee): existe y pertenece a la empresa.
5. **Construir atributos de la nueva lista**:
   - Campos en input no-null → override del valor del source.
   - Campos en input null → inherit del source.
   - `parent_id` = NULL (siempre, es lista base).
   - `archived` = false (siempre, independiente del flag del source).
   - `company_id` = scope del caller.
   - `created_by` = usuario actual.
6. **Excluded payment methods**:
   - Si `excludedPaymentMethodIds` == null → copiar los del source.
   - Si se provee (incluso `[]`) → usar los del input (full replace).
7. **Insertar** la nueva lista en `price_lists` y obtener su ID.
8. **Si `copyDetails == true`** (default): ejecutar en una sola sentencia SQL
   ```sql
   INSERT INTO price_list_details (price_list_id, item_id, price, discount_margin,
     profit_margin, minimum_price, inserted_at, updated_at)
   SELECT $new_id, item_id, price, discount_margin, profit_margin, minimum_price,
     NOW(), NOW()
   FROM price_list_details
   WHERE price_list_id = $source_id
   ```
9. **Transaccional**: toda la operación (insert lista + insert details + excluded payment methods) en una sola transacción. Rollback total si cualquier paso falla.
10. **Devolver** `CopyPriceListPayload` con la lista creada, `copiedDetailsCount` = filas insertadas (0 si `copyDetails=false`), `success=true`.

## Comportamiento — `copyPromotion`

1. **Validar permiso**: el usuario actual debe tener `promotion.copy`.
2. **Validar `sourcePromotionId`**: debe existir, no estar archivada, y tener `parent_id IS NOT NULL`.
3. **Resolver parent**:
   - Si `input.parentId` == null → usar `source.parent_id`.
   - Si se provee, validar que sea una price_list base (parent==null), no archivada, de la misma empresa.
4. **Validar `name`**: único entre **todas** las price_lists de la empresa (archivadas o no, base o promo).
5. **Construir atributos de la nueva promoción**:
   - Atributos **estructurales** (siempre heredados del **parent**, no del source ni overridables por input):
     `isTaxable`, `priceListIncludeTax`, `useAlternativeFormula`, `costMode`, `storageId`, `excluded_payment_methods`, `editablePrice`, `listUpdateBehaviorOnCostChange`.
   - Atributos **propios** (heredados del source salvo override):
     `isActive`, `autoApplyDiscount`, `isPublic`, `allowNewUsersAccess`, `startDate`, `endDate`.
   - `parent_id` = resuelto en paso 3.
   - `archived` = false (siempre).
   - `company_id` = scope del caller.
   - `created_by` = usuario actual.
6. **Insertar** + **copiar details** (misma técnica `INSERT ... SELECT` que en `copyPriceList`, filtrando por `price_list_id = $source_promotion_id`).
7. **Transaccional**: igual que `copyPriceList`.
8. **Devolver** `CopyPromotionPayload`.

## Casos de error

Todos los errores se reportan vía el payload (no como GraphQL errors), usando `success: false`, `message`, y `errors: [FieldError]` cuando aplica a un campo específico.

| Caso | `success` | `message` | `errors[].field` |
|---|---|---|---|
| Sin permisos | `false` | "Permission denied" | — |
| `sourcePriceListId` / `sourcePromotionId` no existe | `false` | "Price list not found" | `sourcePriceListId` / `sourcePromotionId` |
| Source archivada | `false` | "Cannot copy an archived price list" | `sourcePriceListId` / `sourcePromotionId` |
| Source es promo en `copyPriceList` | `false` | "Source must be a base price list" | `sourcePriceListId` |
| Source es base en `copyPromotion` | `false` | "Source must be a promotion" | `sourcePromotionId` |
| `name` vacío / solo whitespace | `false` | "Name is required" | `name` |
| `name` duplicado | `false` | "Name already exists" | `name` |
| `parentId` provisto pero no existe | `false` | "Parent price list not found" | `parentId` |
| `parentId` provisto pero no es base | `false` | "parentId must refer to a base price list" | `parentId` |
| `parentId` archivado | `false` | "Parent price list is archived" | `parentId` |
| `storageId` no existe | `false` | "Storage not found" | `storageId` |
| Algún `excludedPaymentMethodIds` no existe | `false` | "Payment method not found" | `excludedPaymentMethodIds` |
| Item del source eliminado durante la copia | `false` | "Copy failed: source data changed, transaction rolled back" | — |
| Falla técnica / DB error | `false` | "Copy failed, rolled back" | — |

## Permisos

Introducir **dos nuevos códigos de permiso** específicos para copia — semánticamente distintos de "crear desde cero" porque la operación duplica data en bloque y puede tener alcance operativo mayor:

- `copyPriceList` → requiere `price_list.copy`.
- `copyPromotion` → requiere `promotion.copy`.

**Alta del permiso en el backend**: agregar los códigos `price_list.copy` y `promotion.copy` al catálogo de permisos, migrar los roles que corresponda, y exponerlos vía el endpoint de permisos que consume el cliente. El cliente .NET ERP agregará los códigos correspondientes en `NetErp/Helpers/PermissionCodes.cs` (clases `PriceList.Copy` y `Promotion.Copy`).

## Performance y atomicidad

- **Copia de details con `INSERT ... SELECT`**: una sola sentencia SQL. Para listas de 50k+ items la diferencia vs iterar en app es de minutos a milisegundos.
- **Transaccional**: toda la mutación en una transacción única; rollback total ante cualquier fallo (incluyendo modificaciones concurrentes del source).
- **Locking**: `SELECT ... FOR SHARE` sobre el source opcional, según carga real observada.
- **Timeout**: considerar timeout generoso (30-60s) para listas grandes.
- **Sin idempotency key**: la unicidad de `name` previene duplicados por doble-click.

## Ejemplos de uso

### Copiar una lista base, sobrescribiendo nombre y marcándola inactiva

```graphql
mutation {
  copyPriceList(input: {
    sourcePriceListId: "123"
    name: "Lista PÚBLICA 2026 (copia)"
    isActive: false
    copyDetails: true
  }) {
    priceList { id name isActive parent { id } }
    copiedDetailsCount
    success
    message
    errors { field message }
  }
}
```

### Copiar una promoción al mismo parent con nuevas fechas

```graphql
mutation {
  copyPromotion(input: {
    sourcePromotionId: "456"
    name: "Black Friday 2026"
    startDate: "2026-11-27T00:00:00-05:00"
    endDate: "2026-11-30T23:59:59-05:00"
  }) {
    priceList { id name parent { id name } startDate endDate }
    copiedDetailsCount
    success
    message
  }
}
```

### Copiar una promoción a otro parent

```graphql
mutation {
  copyPromotion(input: {
    sourcePromotionId: "456"
    parentId: "789"
    name: "Descuentos mayoristas Q4"
  }) {
    priceList { id name parent { id } }
    copiedDetailsCount
    success
    message
  }
}
```

### Copiar una lista base reemplazando los métodos de pago excluidos

```graphql
mutation {
  copyPriceList(input: {
    sourcePriceListId: "123"
    name: "Lista SOLO EFECTIVO"
    excludedPaymentMethodIds: ["2", "3", "4"]
  }) {
    priceList { id name excludedPaymentMethods { id name } }
    copiedDetailsCount
    success
  }
}
```

### Copiar una lista base sin details (crear skeleton vacío)

```graphql
mutation {
  copyPriceList(input: {
    sourcePriceListId: "123"
    name: "Lista 2027 (vacía)"
    copyDetails: false
  }) {
    priceList { id name }
    copiedDetailsCount   # será 0
    success
  }
}
```
