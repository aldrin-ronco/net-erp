# Sistema de Perfiles y Permisos

## Objetivo

Controlar el acceso de usuarios a las opciones del menu y las acciones que pueden realizar dentro de cada opcion. El sistema opera en dos niveles:

1. **Perfiles de acceso**: determinan A QUE opciones del menu puede entrar un usuario
2. **Permisos**: determinan QUE PUEDE HACER el usuario dentro de cada opcion

## Modelo de Datos

### Diagrama de Relaciones

```
                    ┌──────────────────┐
                    │    companies     │
                    │  (empresa)       │
                    └────────┬─────────┘
                             │ company_id
          ┌──────────────────┼──────────────────────────┐
          │                  │                          │
          ▼                  ▼                          ▼
┌─────────────────┐ ┌──────────────────┐  ┌─────────────────────────┐
│ access_profiles │ │ permission       │  │ company_permission      │
│ (perfiles)      │ │ _definitions     │  │ _defaults               │
│                 │ │ (catalogo de     │  │ (defaults por empresa)  │
│ - name          │ │  permisos)       │  │                         │
│ - is_system_    │ │                  │  │ - default_value         │
│   admin         │ │ - code           │  │   (allowed/denied)      │
└──┬──────────┬───┘ │ - permission_    │  └─────────────────────────┘
   │          │     │   type           │      │               ▲
   │          │     │ - system_default │      │               │
   ▼          ▼     │ - entity_name   │      │  permission_definition_id
┌────────┐ ┌──────┐│ - field_name    │      │               │
│ access │ │access││ - display_order │◄─────┘               │
│_profile│ │_prof.││ - menu_item_id  │──────────────────────►│
│_users  │ │_menu ││                  │                      │
│        │ │_items│└────────┬─────────┘                      │
│-account│ │      │         │                                │
│ _id    │ │-menu │         │ permission_definition_id       │
│        │ │ _item│         ▼                                │
└────────┘ │ _id  │ ┌──────────────────┐                     │
           └──────┘ │ user_permissions │─────────────────────┘
                    │ (permisos por    │
                    │  usuario)        │
                    │                  │
                    │ - account_id     │
                    │ - value          │
                    │   (allowed/      │
                    │    denied)       │
                    │ - expires_at     │
                    └──────────────────┘
```

### Tablas

#### 1. `global.access_profiles` — Perfiles de Acceso

Define grupos de acceso al menu. Un perfil agrupa opciones del menu a las que un usuario puede entrar.

| Campo | Tipo | Descripcion |
|-------|------|-------------|
| id | bigint (identity) | Identificador unico |
| name | varchar(100) | Nombre del perfil (unico por empresa) |
| description | varchar(255) | Descripcion del perfil |
| is_system_admin | boolean | Perfil de administrador del sistema (protegido, inmutable) |
| company_id | bigint FK | Empresa a la que pertenece |
| created_by_id | bigint FK | Usuario que lo creo |

**Reglas**:
- `is_system_admin = true` no puede ser editado ni eliminado via API
- Solo un administrador del sistema puede designar a otro usuario como administrador
- Nombre unico por empresa

#### 2. `global.access_profile_users` — Perfil ↔ Usuario

Asigna perfiles a usuarios. Un usuario puede tener multiples perfiles. El resultado es la UNION de todos los perfiles asignados.

| Campo | Tipo | Descripcion |
|-------|------|-------------|
| id | bigint (identity) | Identificador unico |
| access_profile_id | bigint FK | Perfil asignado |
| account_id | bigint FK | Usuario al que se asigna |
| company_id | bigint FK | Empresa |
| created_by_id | bigint FK | Quien realizo la asignacion |

**Reglas**:
- Un usuario puede tener multiples perfiles (la union de sus accesos)
- Unique constraint: (access_profile_id, account_id, company_id)
- Validacion cross-company: el perfil debe pertenecer a la misma empresa

#### 3. `global.access_profile_menu_items` — Perfil ↔ Opcion de Menu

Define que opciones del menu incluye cada perfil.

| Campo | Tipo | Descripcion |
|-------|------|-------------|
| id | bigint (identity) | Identificador unico |
| access_profile_id | bigint FK | Perfil |
| menu_item_id | bigint FK | Opcion del menu |
| company_id | bigint FK | Empresa |
| created_by_id | bigint FK | Quien realizo la asignacion |

**Reglas**:
- Unique constraint: (access_profile_id, menu_item_id)
- Validacion cross-company: tanto el perfil como el menu item deben pertenecer a la misma empresa
- Solo soporta create y delete (no update — es una asignacion binaria)

#### 4. `global.permission_definitions` — Catalogo de Permisos

Define TODOS los permisos disponibles en el sistema. Se seedea durante provisioning y se actualiza con deploys. No es creado por usuarios finales.

| Campo | Tipo | Descripcion |
|-------|------|-------------|
| id | bigint (identity) | Identificador unico |
| code | varchar(150) | Codigo unico programatico (ej: `CUSTOMER_CREATE`) — inmutable |
| name | varchar(200) | Nombre legible |
| description | varchar(500) | Descripcion detallada |
| menu_item_id | bigint FK | Opcion del menu a la que pertenece |
| company_id | bigint FK | Empresa |
| permission_type | enum | `action` (accion CRUD) o `field` (edicion de campo) |
| entity_name | varchar(100) | Solo para type `field`: entidad (ej: `customer`) |
| field_name | varchar(100) | Solo para type `field`: campo (ej: `credit_limit`) |
| system_default | enum | `allowed` o `denied` — valor cuando no esta establecido |
| display_order | integer | Orden de visualizacion |
| created_by_id | bigint FK | Quien lo creo |

**Reglas**:
- `code` es inmutable despues de creacion
- `code` es unico globalmente (formato: `ENTIDAD_ACCION` o `ENTIDAD_CAMPO_ACCION`)
- Para type `action`: entity_name y field_name deben ser nulos
- Para type `field`: entity_name y field_name son requeridos
- `system_default` define el valor cuando el permiso NO esta establecido para un usuario

#### 5. `global.company_permission_defaults` — Defaults por Empresa

Override del `system_default` de un permiso para una empresa especifica. Usuarios nuevos y permisos sin establecer toman estos valores.

| Campo | Tipo | Descripcion |
|-------|------|-------------|
| id | bigint (identity) | Identificador unico |
| company_id | bigint FK | Empresa |
| permission_definition_id | bigint FK | Definicion del permiso |
| default_value | enum | `allowed` o `denied` |
| created_by_id | bigint FK | Quien lo configuro |

**Reglas**:
- Unique constraint: (company_id, permission_definition_id)
- Un default por empresa por permiso
- Si no existe registro, se usa `permission_definitions.system_default`

#### 6. `global.user_permissions` — Permisos por Usuario

Override individual. Tiene precedencia sobre todos los defaults.

| Campo | Tipo | Descripcion |
|-------|------|-------------|
| id | bigint (identity) | Identificador unico |
| account_id | bigint FK | Usuario |
| permission_definition_id | bigint FK | Definicion del permiso |
| company_id | bigint FK | Empresa |
| value | enum | `allowed` o `denied` |
| expires_at | timestamptz | Null = permanente, fecha = permiso temporal |
| created_by_id | bigint FK | Quien lo configuro |

**Reglas**:
- Unique constraint: (account_id, permission_definition_id, company_id)
- `expires_at` debe ser una fecha futura cuando se establece
- Permisos expirados se tratan como "sin establecer" (caen al default)
- 3 estados logicos: `allowed` (explicito), `denied` (explicito), ausencia de registro (sin establecer → default)

## Logica de Resolucion de Permisos

### Acceso al Menu

```
¿El usuario es system_admin (tiene un perfil con is_system_admin = true)?
  → SI: acceso a TODAS las opciones del menu
  → NO: union de todas las opciones de todos sus perfiles
         → Si la opcion esta en algun perfil → tiene acceso
         → Si no → no puede entrar
```

### Acciones y Campos

```
¿El usuario es system_admin?
  → SI: TODO permitido, sin excepcion

¿Tiene user_permissions para (usuario, permiso, empresa)?
  → SI, y no ha expirado → usar value (allowed/denied)
  → SI, pero expiro → tratar como sin establecer (continuar)

¿Tiene company_permission_defaults para (empresa, permiso)?
  → SI → usar default_value

Usar permission_definitions.system_default
```

### Diagrama de Resolucion

```
                    ┌─────────────────┐
                    │ ¿Es system      │
                    │   admin?        │
                    └────┬──────┬─────┘
                    SI   │      │ NO
                    ▼    │      ▼
              ┌──────┐   │ ┌──────────────────┐
              │PERMIT│   │ │ user_permissions  │
              │  ALL │   │ │ para este usuario │
              └──────┘   │ └────┬────────┬─────┘
                         │ EXISTE│   NO EXISTE
                         │      ▼        ▼
                         │ ┌────────┐ ┌──────────────────┐
                         │ │¿Expiró?│ │ company_permission│
                         │ └─┬────┬─┘ │ _defaults para    │
                         │ SI│  NO│   │ esta empresa      │
                         │   ▼    ▼   └────┬────────┬─────┘
                         │   │  USAR   EXISTE│  NO EXISTE
                         │   │  VALUE       ▼        ▼
                         │   │         ┌────────┐ ┌──────────┐
                         │   └────────►│  USAR  │ │   USAR   │
                         │             │DEFAULT │ │ SYSTEM   │
                         │             │VALUE   │ │ DEFAULT  │
                         │             └────────┘ └──────────┘
```

## Operaciones Masivas

### Asignar todas las autorizaciones a un usuario

```graphql
# Para cada permission_definition existente, crear un user_permission con value: ALLOWED
# Solo aplica a los existentes al momento de la ejecucion
# Permisos futuros se rigen por los defaults
```

### Denegar una accion a multiples usuarios

```graphql
# Ejemplo: denegar eliminacion de clientes a usuarios seleccionados
# Para cada account_id seleccionado:
#   crear/actualizar user_permission con permission_definition.code = "CUSTOMER_DELETE"
#   y value = DENIED
```

### Homologar permisos (snapshot)

```graphql
# Copiar TODOS los user_permissions del usuario origen al usuario destino
# 1. Eliminar todos los user_permissions del destino para esa empresa
# 2. Copiar todos los user_permissions del origen con los mismos values
# Es una copia puntual, no un vinculo permanente
```

### Establecer defaults por empresa

```graphql
# Para cada permission_definition:
#   crear/actualizar company_permission_default con el default_value deseado
# Usuarios nuevos y permisos sin establecer tomaran estos valores
```

## Permisos Temporales

Un permiso con `expires_at` tiene vigencia limitada. Ejemplo: "el usuario X puede aprobar facturas hasta el 15 de abril".

- `expires_at = null` → permiso permanente
- `expires_at = 2026-04-15T23:59:59Z` → permiso vigente hasta esa fecha
- Despues de expirar → el permiso se trata como "sin establecer" (cae al default)
- Los permisos expirados no se eliminan automaticamente (se preservan para auditoria via ExAudit)

## Perfil Administrador del Sistema

- `is_system_admin = true` en access_profiles
- **No puede ser editado ni eliminado** via API
- Bypasea toda la logica de permisos — TODO esta permitido
- Solo un administrador del sistema puede asignar este perfil a otro usuario
- Inmune a asignaciones masivas de denegacion

## Tipos de Permisos

### Action (accion sobre entidad)

Controla operaciones CRUD sobre una entidad:

```
CUSTOMER_CREATE     → Crear cliente (default: allowed)
CUSTOMER_UPDATE     → Editar cliente (default: allowed)
CUSTOMER_DELETE     → Eliminar cliente (default: allowed)
INVOICE_CREATE      → Crear factura (default: allowed)
INVOICE_VOID        → Anular factura (default: denied)
INVENTORY_STOCK_ADJUST → Ajustar inventario (default: denied)
```

### Field (edicion de campo especifico)

Controla si un usuario puede EDITAR un campo sensible. No afecta visibilidad — el usuario siempre puede VER el dato, pero puede no poder editarlo:

```
CUSTOMER_CREDIT_LIMIT_EDIT → Editar limite de credito (default: denied)
PRICE_LIST_PRICE_EDIT      → Editar precio en lista (default: allowed)
```

## Endpoints GraphQL

### Queries

| Query | Descripcion |
|-------|-------------|
| `accessProfilesPage` | Listar perfiles paginados con filtros |
| `accessProfile(id)` | Obtener perfil por ID con usuarios y menu items |
| `accessProfileUsersPage` | Listar asignaciones perfil↔usuario |
| `accessProfileMenuItemsPage` | Listar asignaciones perfil↔menu item |
| `permissionDefinitionsPage` | Listar catalogo de permisos con filtros |
| `permissionDefinition(id)` | Obtener permiso con defaults y user_permissions |
| `companyPermissionDefaultsPage` | Listar defaults por empresa |
| `userPermissionsPage` | Listar permisos de usuario con filtros temporales |

### Mutations

| Mutation | Descripcion |
|----------|-------------|
| `createAccessProfile` | Crear perfil (is_system_admin no expuesto) |
| `updateAccessProfile` | Editar perfil (protegido si is_system_admin) |
| `deleteAccessProfile` | Eliminar perfil (protegido si is_system_admin) |
| `createAccessProfileUser` | Asignar perfil a usuario |
| `deleteAccessProfileUser` | Desasignar perfil de usuario |
| `createAccessProfileMenuItem` | Agregar opcion de menu a perfil |
| `deleteAccessProfileMenuItem` | Remover opcion de menu de perfil |
| `createPermissionDefinition` | Crear definicion de permiso |
| `updatePermissionDefinition` | Editar definicion (code inmutable) |
| `deletePermissionDefinition` | Eliminar definicion de permiso |
| `createCompanyPermissionDefault` | Crear default por empresa |
| `updateCompanyPermissionDefault` | Editar default por empresa |
| `deleteCompanyPermissionDefault` | Eliminar default (cae a system_default) |
| `createUserPermission` | Crear permiso individual para usuario |
| `updateUserPermission` | Editar permiso (value y expires_at) |
| `deleteUserPermission` | Eliminar permiso (cae al default) |

## Notas para Implementacion de UI

1. **Pantalla de perfiles**: CRUD de access_profiles con drag-and-drop de menu_items y lista de usuarios asignados
2. **Pantalla de permisos por usuario**: vista matricial permission_definitions (filas) x value (columnas: allowed/denied/sin establecer) filtrable por menu_item (opcion del menu)
3. **Defaults por empresa**: misma vista matricial pero para company_permission_defaults
4. **Homologacion**: selector de usuario origen → selector de usuario destino → confirmar copia
5. **Asignacion masiva**: selector de permission_definition + value + lista de usuarios → aplicar
6. **Permisos temporales**: campo date-time picker en el formulario de user_permission para expires_at
7. **Indicador visual**: distinguir entre valor explicito (allowed/denied) y valor heredado (sin establecer → default)
8. **Perfil admin**: mostrar badge visual, deshabilitar botones de edicion/eliminacion
