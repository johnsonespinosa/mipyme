# BACKLOG — MiPyME Cuba

> **Leyenda**
> - Prioridad MoSCoW: **M** / **S** / **C** / **W**
> - Estimación: Fibonacci (1, 2, 3, 5, 8, 13)
> - Releases: R0 = Hardening, R1 = Núcleo, R2 = Auth, R3 = Ventas+Pagos+Frontend, R4 = Refinamiento

---

## 🧭 Épicas

| ID   | Épica                                          | Release | Prioridad |
|------|------------------------------------------------|---------|-----------|
| **E0** | **Hardening y Deuda Técnica Inicial**          | R0      | **M**     |
| E1   | Dominio y Modelado                             | R1      | M         |
| E2   | Catálogo de Productos y Categorías             | R1      | M         |
| E3   | Gestión de Inventario                          | R1      | M         |
| E4   | Autenticación y Permisos Completos             | R2      | M         |
| E5   | Carrito y Pedidos                              | R3      | M         |
| E6   | Pagos con Pasarelas Cubanas                    | R3      | M         |
| E7   | Frontend Blazor (Admin/Vendedor)               | R3      | S         |
| E8   | API Pública, Swagger y Versionado              | R1–R4   | M         |
| E9   | Reportes y Notificaciones                      | R4      | S         |
| E10  | Calidad, CI/CD y Despliegue                    | R4      | S         |

---

## E0 · Hardening y Deuda Técnica Inicial (R0 — Siguiente)

> Todas estas US son bloqueantes porque tocan **seguridad** o **calidad base**.

### US-00 · Mover JWT secret a User Secrets
- **Como** operador
- **Quiero** que el `JwtSettings:Secret` no esté en `appsettings.json`
- **Para** no exponer credenciales en el repositorio
- **Prioridad:** **M** · **Estimación:** 1 · **Release:** R0
- **Criterios de aceptación:**
  - `appsettings.json` contiene `"Secret": ""` o `"REPLACE_ME"`.
  - `appsettings.Development.json` referencia vía `dotnet user-secrets`.
  - README documenta cómo configurarlo.
  - `.gitignore` incluye cualquier archivo con secretos locales.

### US-01 · Implementar `PermissionProvider.GetForUserIdAsync`
- **Como** sistema
- **Quiero** que el provider devuelva los permisos reales del usuario
- **Para** que la autorización por permisos funcione
- **Prioridad:** **M** · **Estimación:** 5 · **Release:** R0
- **Criterios de aceptación:**
  - Consulta los permisos del usuario a través de Identity (roles → permisos).
  - Cachea resultados por request.
  - Devuelve `[]` solo si el usuario no existe, nunca por TODO.
  - Cubierto con test unitario.

### US-02 · Completar `PermissionAuthorizationHandler`
- **Como** sistema
- **Quiero** que rechace usuarios no autenticados y evalúe permisos correctamente
- **Para** cerrar la brecha de seguridad actual
- **Prioridad:** **M** · **Estimación:** 3 · **Release:** R0
- **Criterios de aceptación:**
  - Si `context.User.Identity.IsAuthenticated == false` → `Forbid`.
  - Si el usuario no tiene el permiso requerido → `Forbid`.
  - Test unitario de los 3 escenarios (autenticado+permiso, autenticado sin permiso, anónimo).

### US-03 · Proyectos de tests en la solución
- **Como** desarrollador
- **Quiero** `MiPyME.Domain.Tests` y `MiPyME.Application.Tests` en `tests/`
- **Para** tener base para pruebas continuas
- **Prioridad:** **M** · **Estimación:** 3 · **Release:** R0
- **Criterios de aceptación:**
  - Proyectos xUnit + Moq + FluentAssertions.
  - Al menos 1 test placeholder que pase.
  - `dotnet test` ejecuta ambos.

### US-04 · CI mínimo con GitHub Actions
- **Como** equipo
- **Quiero** un workflow que compile y testee en cada push/PR
- **Para** detectar roturas temprano
- **Prioridad:** **M** · **Estimación:** 2 · **Release:** R0
- **Criterios de aceptación:**
  - `.github/workflows/dotnet.yml` ejecuta `dotnet restore`, `dotnet build`, `dotnet test`.
  - Bloquea merge si falla (branch protection).

---

## E1 · Dominio y Modelado (R1)

### US-10 · Value Objects base
- **Como** desarrollador
- **Quiero** `Dinero`, `SKU`, `Direccion` como VOs en Domain
- **Para** encapsular invariantes del negocio
- **Prioridad:** M · **Estimación:** 3 · **Release:** R1
- **Criterios:** inmutables, con validación en constructor (o factory `Create` que devuelve `Result<T>`), tests unitarios.

### US-11 · Entidades del catálogo
- **Como** desarrollador
- **Quiero** entidades `Producto` y `Categoria`
- **Para** modelar el catálogo
- **Prioridad:** M · **Estimación:** 5 · **Release:** R1
- **Criterios:**
  - Heredan de `Entity` (con domain events).
  - `Producto` usa `Dinero` y `SKU` como VOs.
  - Métodos de fábrica `Create(...)` y métodos de dominio (`Activar`, `Desactivar`, `CambiarPrecio`).
  - Tests.

### US-12 · Entidades de inventario
- **Como** desarrollador
- **Quiero** `Inventario` y `MovimientoInventario`
- **Para** modelar el control de stock
- **Prioridad:** M · **Estimación:** 5 · **Release:** R1
- **Criterios:**
  - `Inventario.RegistrarSalida(cantidad)` lanza excepción de dominio si deja stock negativo.
  - `MovimientoInventario` registra tipo, cantidad, motivo, usuario, fecha.
  - Tests unitarios de reglas de stock.

### US-13 · Entidades de pedido
- **Como** desarrollador
- **Quiero** `Pedido`, `ItemPedido`, enum `EstadoPedido`
- **Para** modelar el flujo de venta
- **Prioridad:** M · **Estimación:** 8 · **Release:** R1
- **Criterios:**
  - `Pedido.AgregarItem(producto, cantidad)` congela `precioUnitario`.
  - `Pedido.CambiarEstado(...)` valida transiciones permitidas.
  - `Pedido.Cancelar()` devuelve stock (emite domain event).
  - Tests unitarios.

### US-14 · Entidades de auth y permisos
- **Como** desarrollador
- **Quiero** entidad `Permiso` y `RolePermission`
- **Para** soportar autorización granular
- **Prioridad:** M · **Estimación:** 3 · **Release:** R1
- **Criterios:** nombre único en `Permiso`, asignación a rol, tests.

### US-15 · Configuraciones EF Core
- **Como** desarrollador
- **Quiero** `IEntityTypeConfiguration` para cada entidad
- **Para** mapear el dominio a PostgreSQL con `snake_case`
- **Prioridad:** M · **Estimación:** 5 · **Release:** R1
- **Criterios:**
  - Una `XXXConfiguration : IEntityTypeConfiguration<XXX>` por entidad.
  - VOs mapeados como owned entities.
  - Índices únicos en `Producto.SKU`, `Usuario.Email`, `Permiso.Nombre`.

### US-16 · Migración inicial
- **Como** desarrollador
- **Quiero** una migración `InitialCreate`
- **Para** crear el esquema en PostgreSQL
- **Prioridad:** M · **Estimación:** 2 · **Release:** R1
- **Criterios:** `dotnet ef migrations add InitialCreate` + `dotnet ef database update` exitosos; script SQL revisado.

### US-17 · Seeder de roles, permisos y admin
- **Como** desarrollador
- **Quiero** un seeder idempotente
- **Para** garantizar datos base al iniciar
- **Prioridad:** M · **Estimación:** 3 · **Release:** R1
- **Criterios:** roles `Administrador`, `Vendedor`, `Almacenero`, `Cliente`; permisos base; admin `admin@mipyme.cu`.

---

## E2 · Catálogo de Productos y Categorías (R1)

### US-20 · CRUD de Categorías (Minimal API)
- **Como** Administrador
- **Quiero** CRUD de categorías vía `IEndpoint`
- **Para** organizar el catálogo
- **Prioridad:** M · **Estimación:** 5 · **Release:** R1
- **Criterios:**
  - `CategoriasEndpoints : IEndpoint` con `MapGroup("/api/v1/categorias")`.
  - Handlers de MediatR: `CrearCategoriaCommand`, `ActualizarCategoriaCommand`, `EliminarCategoriaCommand`, `ObtenerCategoriasQuery`, `ObtenerCategoriaPorIdQuery`.
  - Validators con FluentValidation.
  - Permisos: lectura pública, escritura `categorias:write`.
  - No eliminar con productos (`409`).
  - Documentado en Swagger.

### US-21 · CRUD de Productos (Minimal API)
- **Como** Vendedor
- **Quiero** CRUD de productos vía `IEndpoint`
- **Para** gestionar el catálogo
- **Prioridad:** M · **Estimación:** 8 · **Release:** R1
- **Criterios:** análogos a US-20; `ProductosEndpoints`; validación con `Dinero` y `SKU`; permisos `productos:read` / `productos:write`.

### US-22 · Búsqueda y paginación de productos
- **Como** cliente
- **Quiero** filtrar productos
- **Para** encontrar lo que necesito
- **Prioridad:** S · **Estimación:** 5 · **Release:** R1
- **Criterios:** `?page=&pageSize=&search=&categoriaId=&minPrecio=&maxPrecio=`; respuesta paginada; solo activos en público.

---

## E3 · Gestión de Inventario (R1)

### US-30 · Consultar stock por producto
- **Como** Almacenero
- **Quiero** ver stock actual
- **Para** tomar decisiones
- **Prioridad:** M · **Estimación:** 3 · **Release:** R1
- **Criterios:** `GET /api/v1/inventario/{productoId}` devuelve disponible, reservado, mínimo, alerta.

### US-31 · Registrar movimiento de inventario
- **Como** Almacenero
- **Quiero** registrar entradas, salidas y ajustes
- **Para** mantener stock actualizado
- **Prioridad:** M · **Estimación:** 5 · **Release:** R1
- **Criterios:** `POST /api/v1/inventario/movimientos`; valida no dejar stock negativo; audita usuario y motivo.

### US-32 · Historial de movimientos
- **Como** Administrador
- **Quiero** ver el historial con filtros
- **Para** auditar
- **Prioridad:** S · **Estimación:** 3 · **Release:** R1

---

## E4 · Autenticación y Permisos Completos (R2)

### US-40 · Login JWT
- **Como** usuario
- **Quiero** login con email/password
- **Para** obtener JWT
- **Prioridad:** M · **Estimación:** 3 · **Release:** R2
- **Criterios:** `POST /api/v1/auth/login`; 401 genérico si falla; JWT con `sub`, `email`, `permisos`.

### US-41 · Registro de clientes
- **Como** cliente nuevo
- **Quiero** registrarme
- **Para** comprar
- **Prioridad:** M · **Estimación:** 3 · **Release:** R2
- **Criterios:** valida email único y password fuerte; asigna rol `Cliente`.

### US-42 · Refresh token con rotación
- **Como** cliente autenticado
- **Quiero** renovar mi token
- **Para** mantener sesión
- **Prioridad:** S · **Estimación:** 3 · **Release:** R2

### US-43 · CRUD de usuarios
- **Como** Administrador
- **Quiero** gestionar usuarios
- **Para** controlar accesos
- **Prioridad:** S · **Estimación:** 5 · **Release:** R2
- **Criterios:** deshabilitar (no borrar) usuarios con pedidos; cambio de rol auditado.

### US-44 · CRUD de permisos y asignación a roles
- **Como** Administrador
- **Quiero** gestionar permisos y asignarlos a roles
- **Para** tener autorización granular
- **Prioridad:** S · **Estimación:** 5 · **Release:** R2

---

## E5 · Carrito y Pedidos (R3)

### US-50 · Carrito persistido
- **Como** Cliente
- **Quiero** gestionar un carrito
- **Para** preparar mi compra
- **Prioridad:** M · **Estimación:** 5 · **Release:** R3
- **Criterios:** no permite agregar más cantidad que el stock disponible.

### US-51 · Crear pedido desde el carrito
- **Como** Cliente
- **Quiero** confirmar el carrito
- **Para** iniciar el pago
- **Prioridad:** M · **Estimación:** 8 · **Release:** R3
- **Criterios:** congela precio; reserva stock; estado `Pendiente`.

### US-52 · Transiciones de estado del pedido
- **Como** Vendedor/Administrador
- **Quiero** cambiar estado
- **Para** reflejar avance
- **Prioridad:** M · **Estimación:** 5 · **Release:** R3
- **Criterios:** transiciones válidas; inválidas → 400.

### US-53 · Descuento transaccional al pasar a Pagado
- **Como** sistema
- **Quiero** descontar stock transaccionalmente
- **Para** evitar sobreventas
- **Prioridad:** M · **Estimación:** 8 · **Release:** R3
- **Criterios:** transacción de BD; test de concurrencia con 2 pedidos simultáneos del mismo producto.

### US-54 · Cancelar pedido y devolver stock
- **Como** Cliente/Administrador
- **Quiero** cancelar
- **Para** recuperar stock
- **Prioridad:** M · **Estimación:** 3 · **Release:** R3

### US-55 · Ver mis pedidos
- **Como** Cliente
- **Quiero** ver historial
- **Para** seguimiento
- **Prioridad:** M · **Estimación:** 3 · **Release:** R3

---

## E6 · Pagos con Pasarelas Cubanas (R3)

### US-60 · `IPasarelaPagoService` en Application
- **Como** desarrollador
- **Quiero** una abstracción de pasarelas
- **Para** intercambiarlas sin tocar el núcleo
- **Prioridad:** M · **Estimación:** 3 · **Release:** R3

### US-61 · Integración con EnZona
- **Como** Cliente
- **Quiero** pagar con EnZona
- **Para** completar la compra
- **Prioridad:** M · **Estimación:** 8 · **Release:** R3

### US-62 · Webhook de confirmación
- **Como** sistema
- **Quiero** recibir confirmación asíncrona
- **Para** actualizar el pedido
- **Prioridad:** M · **Estimación:** 5 · **Release:** R3
- **Criterios:** valida firma; **idempotente**; si exitoso → pedido `Pagado` + descuento stock.

### US-63 · Resiliencia con Polly
- **Como** sistema
- **Quiero** reintentos en pasarelas
- **Para** tolerar red intermitente
- **Prioridad:** M · **Estimación:** 3 · **Release:** R3

### US-64 · Transfermóvil y TropiPay
- **Como** Cliente
- **Quiero** más opciones de pago
- **Para** elegir la disponible
- **Prioridad:** S · **Estimación:** 13 · **Release:** R3

---

## E7 · Frontend Blazor (R3)

### US-70 · Login Blazor + guard de rutas
- **Como** Administrador
- **Quiero** iniciar sesión en el panel
- **Para** acceder a funciones administrativas
- **Prioridad:** S · **Estimación:** 5 · **Release:** R3
- **Criterios:** JWT en storage; guard por permisos.

### US-71 · CRUD de productos en Blazor
- **Como** Vendedor
- **Quiero** gestionar productos desde la UI
- **Para** no depender de Swagger
- **Prioridad:** S · **Estimación:** 8 · **Release:** R3

### US-72 · Movimientos de inventario en Blazor
- **Como** Almacenero
- **Quiero** registrar movimientos desde la UI
- **Para** operar sin API manual
- **Prioridad:** S · **Estimación:** 5 · **Release:** R3

### US-73 · Listado de pedidos con filtros
- **Como** Vendedor
- **Quiero** ver pedidos y su estado
- **Para** gestionarlos
- **Prioridad:** S · **Estimación:** 5 · **Release:** R3

---

## E8 · API Pública, Swagger y Versionado (transversal)

### US-80 · Swagger completo con `Authorize` funcional
- **Prioridad:** M · **Estimación:** 5 · **Release:** R1
- **Criterios:** XML comments, `ProducesResponseType` en todos los endpoints, botón Authorize probado.

### US-81 · Versionado `/api/v1/`
- **Prioridad:** M · **Estimación:** 3 · **Release:** R1
- **Criterios:** `Asp.Versioning.Mvc` configurado.

### US-82 · Middleware `ProblemDetails` global
- **Prioridad:** M · **Estimación:** 3 · **Release:** R1
- **Criterios:** excepciones de dominio → 400/409; `ValidationException` → 400 con `errors` por campo.

### US-83 · Rate limiting
- **Prioridad:** S · **Estimación:** 3 · **Release:** R4

---

## E9 · Reportes y Notificaciones (R4)

### US-90 · Reporte de ventas por rango de fechas
- **Prioridad:** S · **Estimación:** 5 · **Release:** R4

### US-91 · Reporte de productos más vendidos
- **Prioridad:** S · **Estimación:** 3 · **Release:** R4

### US-92 · Reporte de stock bajo
- **Prioridad:** S · **Estimación:** 2 · **Release:** R4

### US-93 · Email de confirmación de pedido
- **Prioridad:** S · **Estimación:** 3 · **Release:** R4

### US-94 · Email de alerta de stock bajo
- **Prioridad:** C · **Estimación:** 3 · **Release:** R4

---

## E10 · Calidad, CI/CD y Despliegue (R4)

### US-100 · Tests unitarios de Domain
- **Prioridad:** M · **Estimación:** 8 · **Release:** R4
- **Criterios:** cobertura ≥ 70%.

### US-101 · Tests unitarios de Application
- **Prioridad:** M · **Estimación:** 8 · **Release:** R4

### US-102 · Tests de integración (WebApplicationFactory + Testcontainers)
- **Prioridad:** S · **Estimación:** 8 · **Release:** R4

### US-103 · Dockerización (Dockerfile + docker-compose)
- **Prioridad:** S · **Estimación:** 5 · **Release:** R4

### US-104 · Serilog + Seq + correlation-id
- **Estado:** ✅ ya implementado
- **Prioridad:** M · **Release:** R0 (hecho)

### US-105 · Hardening final (vulnerabilidades, HSTS, CORS)
- **Prioridad:** M · **Estimación:** 3 · **Release:** R4

---

## 📊 Resumen por Release

| Release | Épicas incluidas | Nº US | Story Points (aprox.) | Foco |
|---------|------------------|-------|-----------------------|------|
| **R0** | E0 | 5 | ~14 | Hardening + CI + tests base + permisos funcionales |
| **R1** | E1, E2, E3, E8 | 15 | ~60 | Dominio + CRUD + Inventario + Swagger |
| **R2** | E4 | 6 | ~24 | Auth completa + permisos |
| **R3** | E5, E6, E7 | 15 | ~90 | Pedidos + Pagos + Frontend |
| **R4** | E9, E10 | 16 | ~55 | Reportes + Calidad + Ops |
| **TOTAL** | 11 épicas | **57** | **~243** | |

---

## 🎯 Sprint 0 (arranque — sugerido)

Objetivo: **cerrar la deuda técnica bloqueante antes de tocar el dominio**.

| Orden | Historia | Estimación |
|-------|----------|------------|
| 1 | **US-00** · JWT secret a User Secrets | 1 |
| 2 | **US-01** · `PermissionProvider` funcional | 5 |
| 3 | **US-02** · Handler de autorización completo | 3 |
| 4 | **US-03** · Proyectos de tests en la solución | 3 |
| 5 | **US-04** · CI mínimo con GitHub Actions | 2 |
| **Total Sprint 0** | | **14** |

**Al terminar Sprint 0:** el proyecto tiene auth seguro, tests base y CI verde. Puede empezar R1 con el modelado del dominio sin deuda técnica arrastrada.

## ⚠️ Prioridades que no pueden esperar

🔴 **URGENTE — US-00 (JWT secret a User Secrets):** el secreto hardcodeado en `appsettings.json` es un riesgo de seguridad que ya está en el repositorio. Debe moverse antes de cualquier commit nuevo, y si el repo es público, considera rotar el secreto y purgarlo del historial con `git filter-repo` o BFG.

🟠 **US-01 y US-02 (permisos funcionales):** el sistema de autorización está en esqueleto. Sin esto, cualquier endpoint nuevo que uses con `[Authorize(Permission = "...")]` será rechazado o inseguro. Es bloqueante para R1.

🟡 **US-03 y US-04 (tests + CI):** como el dominio está vacío, es el mejor momento para añadir tests y CI. Si esperas a tener 20 handlers sin tests, la deuda se vuelve impagable.
