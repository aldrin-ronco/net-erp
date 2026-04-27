# StorageTransfer — Guía de uso

Traslados de mercancía entre bodegas, multi-destino, con preselección de
lotes/seriales/tallas y flujo de confirmación opcional.

## Tabla de contenidos

- [Conceptos clave](#conceptos-clave)
- [Arquitectura](#arquitectura)
- [Flujo de estados](#flujo-de-estados)
- [API GraphQL](#api-graphql)
  - [Configuración inicial](#configuración-inicial)
  - [Crear draft](#1-crear-draft)
  - [Editar líneas](#2-editar-líneas-del-draft)
  - [Preseleccionar dimensiones](#3-preseleccionar-lotes-seriales-o-tallas)
  - [Postear dispatch](#4-postear-dispatch)
  - [Postear receipts](#5-postear-receipts-solo-modo-confirmado)
  - [Reversar receipts](#6-reversar-un-receipt)
  - [Cancelar](#7-cancelar)
  - [Consultar](#consultas)
- [Ejemplos completos](#ejemplos-completos)
- [Manejo de errores](#manejo-de-errores)
- [Gotchas](#gotchas)

---

## Conceptos clave

### TUI (Transacción Unitaria de Inventario)

Evento físico atómico de stock: **una bodega, un sentido** (entrada o salida),
una o más líneas de ítem. El sentido se deriva del `kardex_flow`
(`I` / `O`) del `accounting_source` asociado.

Los TUI son internos — el usuario nunca los crea directamente. Los crea el
sistema cuando un proceso (StorageTransfer, invoice, etc.) postea.

Cada traslado genera **pares de TUI** (uno `O` + uno `I`):

- **Dispatch**: salida origen + entrada a tránsito (modo confirmado) o a
  destino (modo directo).
- **Receipt** (modo confirmado, 1..N): salida tránsito + entrada a destino.

### Storage Transfer

Documento comercial/logístico del traslado. Tiene:

- 1 origen (`source_storage_id`) compartido por todas las líneas.
- N líneas, cada una con su propio destino (`destination_storage_id`).
- Estado gobernado por la cadena dispatch → receipts → completed.

### Modos

| Modo | `requires_confirmation` | Flujo |
|---|---|---|
| **Directo** | `false` | Dispatch sale de origen y entra al destino en un solo evento. Status: `draft` → `completed`. |
| **Confirmado** | `true` | Dispatch deja mercancía en bodega de tránsito. Receipts posteriores la mueven a destino. Status: `draft` → `in_transit` → `partially_received` → `completed`. |

`requires_confirmation` es **inmutable** después de crear el draft.

### Bodega de tránsito

Única por empresa, configurada en `inventory.configs.transit_storage_id`.
Sólo se usa en modo confirmado. Actúa como buffer visible entre dispatch
y receipt.

### Dimensiones de ítems

Un ítem es **exactamente uno** de estos cuatro tipos:

- **Base** — sin dimensión adicional. Stock en `inventory.stock`.
- **Lot-tracked** (`is_lot_tracked = true`) — por lote. Stock en `stock_by_lot`.
- **Serial-tracked** (`is_serial_tracked = true`) — por unidad serializada. Stock en `stock_by_serial` (qty = 0 o 1).
- **Size-tracked** (`size_category_id != null`) — por talla. Stock en `stock_by_size`.

Mutuamente excluyentes (CHECK constraint en `inventory.items`).

---

## Arquitectura

### Capas (de arriba hacia abajo)

```
┌─────────────────────────────────────┐
│ GraphQL (mutations/queries)         │  ← el cliente llama aquí
├─────────────────────────────────────┤
│ Resolvers                           │
├─────────────────────────────────────┤
│ Inventory.StorageTransfers          │  ← contexto con CRUD + flow ops
├─────────────────────────────────────┤
│ DispatchPoster / ReceiptPoster /    │  ← orquestadores atómicos por op
│ ReceiptReverser                     │
├─────────────────────────────────────┤
│ Inventory.TUI.Poster                │  ← arma payload + invoca motor
├─────────────────────────────────────┤
│ inventory.fn_apply_kardex_batch     │  ← motor PL/pgSQL (single source
│ (PostgreSQL)                        │    of truth para kardex + stock)
└─────────────────────────────────────┘
```

### Tablas principales

- `inventory.storage_transfers` — encabezado del documento.
- `inventory.storage_transfer_lines` — 1 línea por (ítem + destino).
- `inventory.storage_transfer_line_lots/_serials/_sizes` — preselecciones
  dimensionales persistentes durante el draft.
- `inventory.inventory_transactions` — TUI posteado.
- `inventory.inventory_transaction_lines` — detalle del TUI.
- `inventory.inventory_transaction_line_lots/_serials/_sizes` — desglose
  dimensional del TUI.
- `inventory.inventory_transaction_reversals` — vínculo TUI original ↔ reverso.
- `inventory.storage_transfer_tui_links` — puente header ↔ TUI.
- `inventory.storage_transfer_line_tui_line_links` — puente línea ↔ TUI line.

### Preselección persistente (Opción B del diseño)

Para ítems dimensionales, el usuario preselecciona lotes/seriales/tallas
antes del dispatch. Esta selección **persiste en el draft** (no se pierde
si el usuario cierra la sesión). Al postear dispatch, la preselección se
valida (`SUM == dispatched_quantity`) y se materializa en el TUI.

### Auto-numbering

`document_number` se auto-genera vía
`global.fn_next_document_sequence` usando el `document_sequence_id` del
`accounting_source` + el `cost_center_id` del transfer + fecha actual.
Atómico (UPSERT + RETURNING en PostgreSQL, sin huecos, sin duplicados
bajo concurrencia).

El cliente **puede** pasar `document_number` explícito si lo necesita
(imports, migraciones) — en ese caso se respeta.

---

## Flujo de estados

```
                 ┌─────────┐
                 │  draft  │
                 └────┬────┘
                      │ postDispatch
           ┌──────────┴──────────┐
  directo  │                     │ confirmado
           ▼                     ▼
      ┌─────────┐         ┌────────────┐
      │completed│         │ in_transit │◄─── receipts reversos /
      └─────────┘         └─────┬──────┘     cancel header
                                │ postReceipt
                                ▼
                      ┌────────────────────┐
                      │partially_received  │
                      └─────────┬──────────┘
                                │ postReceipt (hasta cubrir todo)
                                ▼
                          ┌──────────┐
                          │completed │ (terminal)
                          └──────────┘

cancelable desde: draft (→ eliminación física)
                  dispatched, in_transit (sin receipts vigentes) → cancelled
                  partially_received CON receipts → rechazado
                  completed → rechazado (obliga traslado inverso)
```

---

## API GraphQL

### Configuración inicial

**Una sola vez por empresa** un admin debe configurar `inventoryConfig` con
5 accounting sources + la bodega de tránsito:

```graphql
mutation ConfigurarTraslados {
  updateInventoryConfig(data: {
    transitStorageId: "42"
    transferDocAccountingSourceId: "10"
    transferSourceOutAccountingSourceId: "11"
    transferDestinationInAccountingSourceId: "12"
    transferTransitInAccountingSourceId: "13"
    transferTransitOutAccountingSourceId: "14"
  }) {
    success
    message
    errors { fields message }
  }
}
```

Las 5 accounting sources tienen roles específicos:

| FK | Rol | `kardex_flow` |
|---|---|---|
| `transferDocAccountingSourceId` | Numeración del documento de traslado. | cualquiera |
| `transferSourceOutAccountingSourceId` | TUI `O` de salida desde origen. | `O` |
| `transferDestinationInAccountingSourceId` | TUI `I` de entrada a destino (receipt o modo directo). | `I` |
| `transferTransitInAccountingSourceId` | TUI `I` de entrada a tránsito (solo modo confirmado). | `I` |
| `transferTransitOutAccountingSourceId` | TUI `O` de salida de tránsito (solo modo confirmado). | `O` |

El servidor valida cross-table que cada FK apunte a un accounting_source
de la misma empresa y con el `kardex_flow` esperado.

---

### 1. Crear draft

```graphql
mutation CrearDraft {
  createStorageTransferDraft(input: {
    accountingSourceId: "10"     # el doc_source configurado arriba
    costCenterId: "5"             # obligatorio
    sourceStorageId: "100"
    requiresConfirmation: true    # inmutable
    note: "Reabasto mensual"
    # documentNumber se auto-genera (opcional pasarlo)
    lines: [
      {
        itemId: "700"
        destinationStorageId: "201"
        dispatchedQuantity: "10"
        displayOrder: 0
      },
      {
        itemId: "701"
        destinationStorageId: "202"
        dispatchedQuantity: "5"
        displayOrder: 1
      }
    ]
  }) {
    storageTransfer {
      id
      documentNumber       # → "26000001" auto-generado
      status               # → "draft"
      requiresConfirmation
    }
    success
    errors { fields message }
  }
}
```

---

### 2. Editar líneas del draft

Patch incremental con mutations dedicadas:

```graphql
# Agregar línea
mutation {
  addStorageTransferDraftLine(input: {
    storageTransferId: "1"
    itemId: "702"
    destinationStorageId: "201"
    dispatchedQuantity: "3"
  }) {
    storageTransferLine { id }
    success
  }
}

# Actualizar línea
mutation {
  updateStorageTransferDraftLine(input: {
    id: "1"
    dispatchedQuantity: "7"
  }) {
    storageTransferLine { id dispatchedQuantity }
    success
  }
}

# Eliminar línea
mutation {
  deleteStorageTransferDraftLine(id: "1") {
    success
  }
}
```

Cambiar `itemId` o `destinationStorageId` de una línea **borra** en cascada
sus preselecciones de lotes/seriales/tallas.

---

### 3. Preseleccionar lotes, seriales o tallas

Para ítems dimensionales — se reemplaza la lista completa en cada llamada.

```graphql
# Lotes
mutation {
  setStorageTransferLineLots(input: {
    storageTransferLineId: "1"
    lots: [
      { lotId: "L01", quantity: "6", displayOrder: 0 }
      { lotId: "L02", quantity: "4", displayOrder: 1 }
    ]
  }) {
    storageTransferLineLots { lotId quantity }
    success
  }
}

# Seriales (quantity es implícita = 1 por serial)
mutation {
  setStorageTransferLineSerials(input: {
    storageTransferLineId: "1"
    serials: [
      { serialId: "S001", displayOrder: 0 }
      { serialId: "S002", displayOrder: 1 }
    ]
  }) { success }
}

# Tallas
mutation {
  setStorageTransferLineSizes(input: {
    storageTransferLineId: "1"
    sizes: [
      { sizeId: "SZ-S", quantity: "3", displayOrder: 0 }
      { sizeId: "SZ-M", quantity: "4", displayOrder: 1 }
    ]
  }) { success }
}
```

**Reglas**:

- Para lotes y tallas: `SUM(quantity)` debe igualar `dispatched_quantity` al
  momento del dispatch. Puedes preseleccionar progresivamente — la suma
  se valida al postear.
- Para seriales: `COUNT` debe igualar `dispatched_quantity`. Un serial no
  puede estar preseleccionado en dos drafts simultáneos (UNIQUE global).
- El ítem debe ser compatible con la dimensión (lot-tracked /
  serial-tracked / size-tracked). El servidor valida.

---

### 4. Postear dispatch

```graphql
mutation {
  postStorageTransferDispatch(id: "1") {
    storageTransfer {
      id
      status               # "completed" (directo) o "in_transit" (confirmado)
      dispatchedAt
      dispatchedById
    }
    success
    errors { fields message }
  }
}
```

**Qué ocurre dentro**:

1. Lock pesimista del transfer y sus líneas.
2. Validación de estado, config, preselecciones.
3. Crea par de TUIs (`O` origen + `I` tránsito/destino).
4. Invoca el motor `fn_apply_kardex_batch` que:
   - Decrementa `stock` / `stock_by_lot` / `stock_by_serial` / `stock_by_size` en origen.
   - Incrementa en destino/tránsito.
   - Escribe filas en `inventory.kardex`.
   - Transiciona `serials.status` según `serial_status_transition` del accounting_source.
5. Aplica costos retornados por el motor a las filas del TUI.
6. Transiciona status del transfer.

**Todo atómico**: cualquier error hace rollback total.

---

### 5. Postear receipts (solo modo confirmado)

Para ítems de dimensión, el receipt replica los lotes/seriales/tallas
recibidos:

```graphql
mutation {
  postStorageTransferReceipt(input: {
    storageTransferId: "1"
    note: "Recepción parcial 1"
    lines: [
      {
        storageTransferLineId: "100"
        quantity: "3"
        # Para ítems dimensionales, especificar qué se recibió:
        lots: [
          { lotId: "L01", quantity: "3" }
        ]
      }
    ]
  }) {
    storageTransfer {
      id
      status               # "partially_received" o "completed"
    }
    success
  }
}
```

**Reglas**:

- `quantity ≤ (dispatched_quantity - received_quantity)` por línea (lo pendiente).
- Para dimensionales: la suma/conteo debe cuadrar con `quantity` del receipt.
- Un receipt genera 1 par de TUIs con `transferLeg = "receipt"` y
  `leg_sequence` correlativo (1, 2, 3…).
- Varios receipts acumulan `received_quantity` en las líneas.
- Cuando `received == dispatched` en todas las líneas, el transfer pasa a
  `completed`.

Diferencia con el dispatch: en receipt **no hay preselección persistente**.
La información de qué se recibió viene en el input de la mutation
(Opción A del diseño — la UI ya muestra lo pendiente, no hace falta
persistir borrador).

---

### 6. Reversar un receipt

```graphql
mutation {
  reverseStorageTransferReceipt(input: {
    inventoryTransactionId: "50"   # el TUI I del receipt (el visible)
  }) {
    storageTransfer {
      id
      status   # retrocede de "completed" a "partially_received" o "in_transit"
    }
    success
  }
}
```

**Reglas**:

- Sólo se reversan receipts. El dispatch no se reversa individualmente.
- Sólo mientras `transfer.status != "completed"`. Una vez completed, hay
  que crear un traslado inverso como documento aparte.
- Un TUI reversado no puede volver a reversarse (guard atómico
  `UPDATE ... WHERE status='posted'`).
- El reverso crea 2 TUIs nuevos con `is_reversal = true` y los vincula
  vía `inventory_transaction_reversals`. `received_quantity` se recalcula.

---

### 7. Cancelar

```graphql
mutation {
  cancelStorageTransfer(input: {
    id: "1"
    note: "Cancelado por error operativo"
  }) {
    storageTransfer {
      id
      status   # "cancelled"
      cancelledAt
      cancelledById
    }
    success
  }
}
```

**Matriz de cancelación**:

| Estado inicial | Resultado |
|---|---|
| `draft` | Eliminación física del documento. |
| `in_transit` sin receipts vigentes | `cancelled` — la mercancía permanece en tránsito (hay que crear traslado inverso para regresarla). |
| `partially_received` / `in_transit` con receipts | Rechazado. Anular receipts primero (uno por uno) y luego cancelar. |
| `completed` | Rechazado. Crear traslado inverso como documento nuevo. |

---

### Consultas

```graphql
# Traslado puntual con todo el grafo
query {
  storageTransfer(id: "1") {
    id
    documentNumber
    status
    requiresConfirmation
    sourceStorage { id name }
    costCenter { id name }
    createdBy { id firstName lastName }
    dispatchedBy { id firstName }
    lines {
      id
      item { id name reference }
      destinationStorage { id name }
      dispatchedQuantity
      receivedQuantity
      pendingQuantity       # derivado
      lotPreselections {
        lot { id lotNumber expirationDate }
        quantity
      }
      serialPreselections {
        serial { id serialNumber }
      }
      sizePreselections {
        size { id value }
        quantity
      }
    }
    tuiLinks {
      transferLeg
      legSequence
      inventoryTransaction {
        id
        status
        accountingSource { id code name }
        lines {
          item { id }
          storage { id name }
          quantity
          unitCost
          totalCost
        }
      }
    }
  }
}

# Listado paginado (con caché del lado servidor)
query {
  storageTransfersPage(
    pagination: { page: 1, pageSize: 20 }
    filters: { status: IN_TRANSIT }
    sort: [{ field: DISPATCHED_AT, direction: DESC }]
  ) {
    entries { id documentNumber status }
    totalEntries
    totalPages
    pageNumber
    pageSize
  }
}

# Historial de TUIs (útil para reportes de kardex)
query {
  inventoryTransactionsPage(
    pagination: { page: 1, pageSize: 50 }
    filters: { status: POSTED }
  ) {
    entries {
      id
      accountingSource { code kardexFlow }
      lines {
        item { id name }
        storage { id name }
        quantity
        unitCost
      }
    }
    totalEntries
  }
}

# Encontrar el reverso de un TUI
query {
  inventoryTransactionReversal(originalTransactionId: "50") {
    reversalTransaction { id status }
    createdBy { id firstName }
    insertedAt
  }
}

# Top-level queries de conveniencia
query {
  storageTransferLines(storageTransferId: "1") { id quantity ... }
  storageTransferLineLots(storageTransferLineId: "100") { lotId quantity }
  storageTransferLineSerials(storageTransferLineId: "100") { serialId }
  storageTransferLineSizes(storageTransferLineId: "100") { sizeId quantity }
}
```

---

## Ejemplos completos

### Ejemplo 1 — Modo directo, ítem base

Traslado inmediato de 3 unidades de un ítem sin dimensión.

```graphql
# 1. Crear draft
mutation {
  createStorageTransferDraft(input: {
    accountingSourceId: "10"
    costCenterId: "5"
    sourceStorageId: "100"
    requiresConfirmation: false
    lines: [{
      itemId: "700"
      destinationStorageId: "201"
      dispatchedQuantity: "3"
      displayOrder: 0
    }]
  }) {
    storageTransfer { id documentNumber status }
  }
}

# Respuesta:
# { id: "1", documentNumber: "26000001", status: "draft" }

# 2. Postear dispatch — termina en un solo paso
mutation {
  postStorageTransferDispatch(id: "1") {
    storageTransfer {
      status            # → "completed"
      completedAt       # → timestamp
    }
  }
}
```

**Efectos**:
- Stock en origen: `-3`.
- Stock en destino: `+3`.
- 2 TUIs creados (O origen + I destino).
- 2 filas en `kardex`.
- `received_quantity = dispatched_quantity` (el sistema lo copia atómicamente).

---

### Ejemplo 2 — Modo confirmado con recepción parcial

Despacho de 5 unidades, recepción en 2 tandas.

```graphql
# 1. Draft
mutation {
  createStorageTransferDraft(input: {
    accountingSourceId: "10"
    costCenterId: "5"
    sourceStorageId: "100"
    requiresConfirmation: true
    lines: [{
      itemId: "700"
      destinationStorageId: "201"
      dispatchedQuantity: "5"
      displayOrder: 0
    }]
  }) { storageTransfer { id } }
}
# id=1

# 2. Dispatch — mercancía sale de origen y queda en tránsito
mutation {
  postStorageTransferDispatch(id: "1") {
    storageTransfer { status }  # "in_transit"
  }
}

# Stock: origen −5, tránsito +5, destino +0

# 3. Primer receipt: 2 unidades
mutation {
  postStorageTransferReceipt(input: {
    storageTransferId: "1"
    lines: [{ storageTransferLineId: "100", quantity: "2" }]
  }) {
    storageTransfer { status }  # "partially_received"
  }
}

# Stock: tránsito −2, destino +2

# 4. Segundo receipt: 3 restantes
mutation {
  postStorageTransferReceipt(input: {
    storageTransferId: "1"
    lines: [{ storageTransferLineId: "100", quantity: "3" }]
  }) {
    storageTransfer { status }  # "completed"
  }
}

# Stock final: tránsito 0, destino 5
```

---

### Ejemplo 3 — Ítem lot-tracked

Traslado de un ítem con 2 lotes distintos.

```graphql
# 1. Draft con cantidad total 10
mutation {
  createStorageTransferDraft(input: {
    accountingSourceId: "10"
    costCenterId: "5"
    sourceStorageId: "100"
    requiresConfirmation: false
    lines: [{
      itemId: "800"
      destinationStorageId: "201"
      dispatchedQuantity: "10"
      displayOrder: 0
    }]
  }) {
    storageTransfer { id }
  }
}

# 2. Preseleccionar lotes: 6 del L01 + 4 del L02
mutation {
  setStorageTransferLineLots(input: {
    storageTransferLineId: "100"
    lots: [
      { lotId: "L01", quantity: "6", displayOrder: 0 }
      { lotId: "L02", quantity: "4", displayOrder: 1 }
    ]
  }) { success }
}

# 3. Dispatch — el motor valida stock por lote
mutation {
  postStorageTransferDispatch(id: "1") {
    storageTransfer { status }  # "completed"
  }
}
```

El TUI resultante tiene una línea parent con `quantity = 10` y **2 sub-filas
en `inventory_transaction_line_lots`** — una por cada lote, con su propio
costo (PEPS por lote).

---

### Ejemplo 4 — Reversar un receipt parcial

Siguiendo del ejemplo 2, si después del primer receipt de 2 unidades
queremos anularlo:

```graphql
# Primero, encontrar el TUI I del receipt
query {
  storageTransfer(id: "1") {
    tuiLinks(transferLeg: RECEIPT) {
      legSequence
      inventoryTransaction {
        id
        accountingSource { code }   # buscar el que tiene kardex_flow=I
      }
    }
  }
}

# Supongamos el TUI I del receipt es id=25

mutation {
  reverseStorageTransferReceipt(input: { inventoryTransactionId: "25" }) {
    storageTransfer {
      status   # "in_transit" (retrocede)
    }
  }
}
```

**Efectos**:
- Stock: destino −2 (vuelve), tránsito +2.
- Se crean 2 TUIs nuevos con `is_reversal = true`.
- Los 2 TUIs del receipt original pasan a `status = "reversed"`.
- `received_quantity` de la línea vuelve a `0`.

---

## Manejo de errores

Todas las mutaciones retornan un payload con `success`, `message` y `errors`.

### Shape de errores comunes

```graphql
{
  errors: [
    { fields: ["dispatched_quantity"], message: "debe ser mayor que 0" }
    { fields: ["status"], message: "solo un traslado en estado draft puede despacharse (actual: completed)" }
    { fields: ["lots"], message: "la preselección (3) no coincide con dispatched_quantity (5)" }
  ]
}
```

### Errores típicos

| Situación | Field | Mensaje |
|---|---|---|
| Stock insuficiente | `base` | `INSUFFICIENT_STOCK` (del motor PL/pgSQL) |
| Preselección incompleta | `lots` / `serials` / `sizes` | `la preselección no coincide con dispatched_quantity` |
| Dispatch sobre transfer completed | `status` | `solo un traslado en estado draft puede despacharse` |
| Receipt excede pendiente | `quantity` | `excede lo pendiente por recibir (N) en la línea X` |
| Config incompleto | `transfer_*_accounting_source_id` | `debe estar configurado en inventory_configs` |
| Reverse sobre completed | `status` | `no se pueden anular recepciones de un traslado ya completado` |
| Double reverse | `status` | `el receipt ya fue reversado previamente` |
| Sin `reverse_accounting_source_id` | `transfer_destination_in_accounting_source_id` | `la fuente contable no tiene configurada su fuente de anulación` |
| Ítem no maneja dimensión | `item_id` | `el ítem no maneja lotes` (o seriales / tallas) |

---

## Gotchas

1. **`cost_center_id` es obligatorio.** Todo documento administrativo del
   sistema lo requiere — se usa para numeración (forma parte de la UNIQUE
   en `document_sequence_*`) y contabilidad.

2. **`requires_confirmation` es inmutable.** Si te equivocaste al crear el
   draft, tienes que eliminarlo y crear uno nuevo.

3. **El documento se cancela, la mercancía no.** Cancelar un transfer
   `in_transit` lo marca como `cancelled` pero no retorna la mercancía al
   origen — queda en tránsito para siempre (o hasta que se cree un
   traslado inverso como documento nuevo). Esto es intencional: preserva
   inmutabilidad del kardex.

4. **Los seriales son únicos globales.** Un mismo `serialId` no puede
   estar preseleccionado en dos drafts simultáneamente (UNIQUE
   `(company_id, serial_id)` en `storage_transfer_line_serials`). Si
   necesitas el serial en otro draft, primero elimina la preselección
   del primero.

5. **Cambiar item/destino en una línea draft borra las preselecciones.**
   Cascade delete automático sobre `_line_lots`/`_line_serials`/
   `_line_sizes`. No hay advertencia — el sistema asume que si cambias
   la línea, las preselecciones ya no aplican.

6. **La bodega de tránsito es única por empresa.** No hay bodegas de
   tránsito por ruta o por zona. Todos los traslados confirmados de la
   empresa pasan por la misma bodega virtual de tránsito.

7. **Los traslados directos marcan `received_quantity = dispatched_quantity`
   automáticamente.** No hay receipt separado — dispatch y recepción son
   atómicos en el mismo evento.

8. **El auto-numbering requiere que el `accounting_source` tenga
   `document_sequence_id` configurado.** Si falla, pasa `documentNumber`
   explícito en el input o configura la secuencia primero.

9. **`reverse_accounting_source_id` se auto-pobla.** El `MainStep` de
   `AccountingSources` crea automáticamente las 2 variantes de anulación
   (A/X) y linkea el reverso. No deberías necesitar configurarlo manual
   — si el reverso falla por esta razón, hay un problema de data en el
   accounting_source.

10. **Los TUI no se modifican.** Una vez posteados, son inmutables. Para
    "deshacer" hay que reversarlos, lo cual crea TUIs nuevos con
    `is_reversal = true`. El kardex siempre crece, nunca se edita.

---

## Referencia rápida

**Mutations** (13 en total):

| Mutation | Qué hace |
|---|---|
| `createStorageTransferDraft` | Crea draft + líneas iniciales. |
| `updateStorageTransferDraft` | Actualiza campos editables del header (note). |
| `deleteStorageTransferDraft` | Elimina draft (solo si `status = draft`). |
| `addStorageTransferDraftLine` | Agrega línea al draft. |
| `updateStorageTransferDraftLine` | Actualiza línea del draft. |
| `deleteStorageTransferDraftLine` | Elimina línea del draft. |
| `setStorageTransferLineLots` | Reemplaza preselección de lotes de una línea. |
| `setStorageTransferLineSerials` | Reemplaza preselección de seriales. |
| `setStorageTransferLineSizes` | Reemplaza preselección de tallas. |
| `postStorageTransferDispatch` | Postea el dispatch (arma los TUIs, afecta stock). |
| `postStorageTransferReceipt` | Postea una recepción parcial o total. |
| `reverseStorageTransferReceipt` | Anula un receipt. |
| `cancelStorageTransfer` | Elimina draft o cancela transfer sin receipts vigentes. |

**Queries** (6 en total):

| Query | Qué retorna |
|---|---|
| `storageTransfer(id)` | Transfer puntual con todo su grafo (dataloader). |
| `storageTransfersPage(pagination, filters, sort)` | Listado paginado y cacheado. |
| `storageTransferLines(storageTransferId)` | Líneas del transfer (conveniencia). |
| `storageTransferLineLots/Serials/Sizes(storageTransferLineId)` | Preselecciones de una línea. |
| `inventoryTransaction(id)` / `inventoryTransactionsPage` | Historial de TUIs. |
| `inventoryTransactionReversal(originalTransactionId)` | Navegar del TUI original a su reverso. |

---

*Última actualización: 2026-04-23.*
