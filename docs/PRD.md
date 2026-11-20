# PRD — MiPyME Cuba v2.0

**Versión:** 2.0  
**Estado:** Aprobado para desarrollo  
**Fecha:** 2026-11-20  
**Autor:** Johnson Espinosa  
**Repositorio:** https://github.com/johnsonespinosa/mipyme  
**Stack:** .NET 8 · Clean Architecture (6 proyectos) · EF Core 8 · PostgreSQL  
**Audiencia:** Equipo de desarrollo, Product Owner, stakeholders

---

## 1. Resumen Ejecutivo

MiPyME Cuba es una plataforma de comercio electrónico en línea para las MiPyMES cubanas. Su objetivo es permitir a pequeños comercios gestionar catálogo, inventario, pedidos y cobros con pasarelas de pago locales (Transfermóvil, EnZona, TropiPay), mediante una API REST y una interfaz Blazor WebAssembly para administradores y vendedores.

**Estado técnico actual:** la solución compila (0 warnings, 0 errors) con toda la infraestructura de capas, CQRS, pipeline HTTP, autenticación JWT (esqueleto) y Blazor WASM (plantilla por defecto). El dominio está vacío y no hay casos de uso ni endpoints de negocio. Está lista para recibir el PRD.

---

## 2. Contexto y Problema

**Situación actual:**

- Miles de MiPyMES cubanas operan con WhatsApp, hojas de cálculo o cuadernos.
- No existe una solución de e-commerce accesible, localizada y asequible.
- Las pasarelas locales (Transfermóvil, EnZona) tienen APIs, pero su integración es compleja para MiPyMES sin TI.
- La conectividad intermitente inviabiliza muchas soluciones cloud internacionales.

**Problema:**

"Las MiPyMES cubanas no tienen una herramienta digital integrada, accesible y localizada para vender en línea, gestionar inventario y cobrar con pasarelas locales."

---

## 3. Objetivos (SMART)

| ID | Objetivo | Métrica de Éxito | Release |
|----|----------|------------------|---------|
| O1 | Modelar el dominio completo de e-commerce e inventario | 6 entidades + 3 VOs + 1 enum implementados | R1 |
| O2 | Exponer CRUD funcional de productos y categorías vía Minimal APIs | Endpoints verificados en Swagger | R1 |
| O3 | Implementar gestión de inventario con auditoría | Movimientos trazables, cero stock negativo | R1 |
| O4 | Completar el sistema de auth por permisos (no solo roles) | PermissionProvider funcional + cobertura tests | R2 |
| O5 | Implementar pedidos con descuento transaccional de stock | Cero sobreventas en pruebas de concurrencia | R2 |
| O6 | Integrar al menos 1 pasarela cubana end-to-end | Pago confirmado en sandbox | R3 |
| O7 | Ofrecer frontend Blazor funcional para Administrador/Vendedor | CRUD de productos usable desde la UI | R3 |
| O8 | Garantizar calidad con tests y CI/CD | ≥ 70% cobertura Domain/Application; CI verde | R4 |

---

## 4. Alcance

**✅ Dentro del alcance**

Backend (API REST + Minimal APIs):

- Auth JWT con permisos granulares (no solo roles).
- CRUD de Productos, Categorías.
- Gestión de Inventario con auditoría.
- Carrito y Pedidos con estados.
- Integración con Transfermóvil, EnZona, TropiPay.
- Webhooks de confirmación de pago.
- Reportes básicos.
- Documentación Swagger completa.

Frontend (Blazor WASM):

- Panel de administración: gestión de productos, categorías, inventario.
- Panel de vendedor: ver pedidos, actualizar estados.

**NO incluye:** tienda pública de cara al cliente final (esa sería una app móvil o web separada en el futuro).

Transversal:

- SharedKernel con `Result<T>`, `Error`, `Entity`, domain events (ya existe).
- Serilog + Seq + correlation-id.
- HealthChecks.

**❌ Fuera del alcance**

- Envío físico / logística.
- Facturación fiscal formal (ONAT).
- Multi-tenant.
- Gestión de proveedores.
- CRM / marketing automation.
- Fidelización con puntos/cupones.
- Tienda pública para el cliente final (se cubre con la API para apps móviles futuras).

---

## 5. Personas

| Persona | Rol / Permisos | Necesidades clave |
|---------|----------------|-------------------|
| Ana, dueña | Administrador (todos los permisos) | Visión global, gestión de usuarios, reportes, config de pasarelas. |
| Luis, vendedor | `productos:read/write`, `pedidos:read/write` | Publicar productos, actualizar precios, gestionar pedidos. |
| Carla, almacenera | `inventario:read/write` | Movimientos de stock, alertas de reposición. |
| Pedro, cliente | `pedidos:read/write` (propios), `carrito:*` | Catálogo, carrito, pago, seguimiento. |
| Integrador externo | API key / JWT | Consumir la API desde app móvil propia. |

**Nota:** la autorización es por permisos, no solo por roles. Un rol agrupa permisos. Esto permite crear roles personalizados sin tocar código.

---

## 6. Requisitos Funcionales (por módulo)

### RF-01 · Autenticación y Permisos

- **RF-01.1:** Login JWT → `{ accessToken, refreshToken }`.
- **RF-01.2:** Registro de clientes.
- **RF-01.3:** Refresh token con rotación.
- **RF-01.4:** CRUD de usuarios (solo Administrador).
- **RF-01.5:** Asignación de permisos a roles; roles a usuarios.
- **RF-01.6:** `PermissionProvider.GetForUserIdAsync` implementado (actualmente TODO).
- **RF-01.7:** `PermissionAuthorizationHandler` rechaza usuarios no autenticados correctamente (actualmente TODO).
- **RF-01.8:** Seeder de roles + permisos + admin idempotente.

### RF-02 · Productos y Categorías

- CRUD de categorías (jerárquicas opcionales).
- CRUD de productos: nombre, descripción, `Dinero`, `SKU`, categoría, imagen, estado.
- Búsqueda y filtrado con paginación.
- No eliminar categoría con productos.

### RF-03 · Inventario

- Entidad `Inventario` con `disponible`, `reservado`, `mínimo`.
- Movimientos (entrada, salida, ajuste) auditados.
- No salida con stock negativo.
- Alertas por stock mínimo.

### RF-04 · Pedidos

- Carrito persistido.
- Pedido congela precio unitario.
- Transiciones: `Pendiente → Pagado → Enviado → Entregado`; `→ Cancelado` antes de `Enviado`.
- Descuento transaccional de stock al pasar a `Pagado`.
- Cancelación devuelve stock.

### RF-05 · Pagos

- `IPasarelaPagoService` en Application.
- Implementaciones por pasarela en Infrastructure.
- Webhooks validados por firma e idempotentes.
- Auditoría de intentos.

### RF-06 · Frontend Blazor

- **RF-06.1:** Panel de administración con CRUD de productos y categorías.
- **RF-06.2:** Panel de inventario con movimientos.
- **RF-06.3:** Listado de pedidos con filtros por estado.
- **RF-06.4:** Vista de detalle de pedido.
- **RF-06.5:** Login con JWT persistido en `localStorage` / `sessionStorage`.
- **RF-06.6:** Guard de rutas por permisos.

### RF-07 · API Pública

- **RF-07.1:** Endpoints documentados en Swagger con ejemplos.
- **RF-07.2:** Versionado `/api/v1/`.
- **RF-07.3:** Rate limiting.
- **RF-07.4:** Respuestas `ProblemDetails`.

### RF-08 · Notificaciones

- Emails de registro, pedido, stock bajo (cola asíncrona).

---

## 7. Requisitos No Funcionales (RNF)

| ID | Categoría | Requisito |
|----|-----------|-----------|
| RNF-01 | Rendimiento | Listados paginados p95 < 300 ms con 10k productos. |
| RNF-02 | Seguridad | JWT secret movido a User Secrets (actualmente hardcodeado). |
| RNF-03 | Seguridad | Contraseñas hasheadas con Identity. |
| RNF-04 | Seguridad | Autorización por permisos, no solo roles. |
| RNF-05 | Seguridad | `PermissionProvider` y handler de auth completos (TODOs resueltos). |
| RNF-06 | Resiliencia | Polly en llamadas a pasarelas (reintentos + circuit breaker). |
| RNF-07 | Tolerancia a red | Operaciones no críticas (emails) encoladas. |
| RNF-08 | Observabilidad | Serilog + Seq + correlation-id (ya existe). |
| RNF-09 | Mantenibilidad | Domain con cero dependencias externas; SharedKernel como base. |
| RNF-10 | Testabilidad | Proyectos de test en la solución (tests/ hoy vacío). |
| RNF-11 | Documentación | 100% endpoints con `[ProducesResponseType]` y XML comments. |
| RNF-12 | Compatibilidad | PostgreSQL 14+; snake_case naming; EF Core 8. |

---

## 8. Arquitectura Técnica (actualizada)

**Proyectos (6) en `MIPYME.sln`:**

```
MIPYME.sln
├── src/
│   ├── MiPyME.Domain                    ← Entidades, VOs, eventos de dominio (VACÍO)
│   ├── MiPyME.Application               ← CQRS base, behaviors, validators (sin casos de uso)
│   ├── MiPyME.Infrastructure            ← EF Core AppDbContext, migraciones (sin entidades)
│   ├── MiPyME.Presentation.Api          ← ASP.NET Core Minimal APIs (IEndpoint), Swagger, JWT
│   ├── MiPyME.Presentation.App          ← Blazor WASM (template Home/Counter/Weather)
│   └── MiPyME.SharedKernel              ← Entity, Result<T>, Error, IDateTimeProvider, domain events
├── tests/                               ← VACÍO
└── MIPYME.sln
```

**Patrones y stack aplicados:**

- Minimal APIs con patrón `IEndpoint`: cada grupo de endpoints es una clase que implementa `IEndpoint` y se auto-registra.
- CQRS con MediatR: `ICommand` / `IQuery`, `LoggingBehavior`, `ValidationBehavior` (FluentValidation).
- Autorización por permisos: `PermissionRequirement`, `PermissionAuthorizationHandler`, `PermissionAuthorizationPolicyProvider` (esqueleto con TODOs).
- SharedKernel: `Entity` con domain events, `Result<T>`, `Error`, `ErrorType`, `ValidationError`, `IDateTimeProvider`, `IDomainEvent`.
- EF Core 8: snake_case naming, schema `public`, publicación de domain events desde el DbContext, migraciones automáticas en Development.
- Observabilidad: Serilog (Console + Seq), correlation-id middleware, global exception handler.
- Frontend: Blazor WASM (por definir su alcance funcional).

---

## 9. Modelo de Datos de Alto Nivel

**Entidades:** `Producto`, `Categoria`, `Inventario`, `MovimientoInventario`, `Pedido`, `ItemPedido`, `Pago`, `Usuario` (IdentityUser).

**Value Objects:** `Dinero`, `Direccion`, `SKU`.

**Enum:** `EstadoPedido`.

Además: permisos como entidad de dominio (`Permiso` + tabla `RolePermission`).

---

## 10. Integraciones Externas

| Servicio | Tipo | Uso | Release |
|----------|------|-----|---------|
| Transfermóvil | Pasarela de pago | Cobro a clientes | R3 |
| EnZona | Pasarela de pago | Cobro con tarjetas CUP/USD | R3 |
| TropiPay | Pasarela de pago | Pagos online y remesas | R3 |
| SMTP (email) | Notificación | Confirmaciones, alertas | R4 |

---

## 11. Riesgos

| ID | Riesgo | Prob. | Imp. | Mitigación |
|----|--------|-------|------|------------|
| R-01 | Cambios en API de pasarelas cubanas | Media | Alto | `IPasarelaPagoService` + pruebas de contrato. |
| R-02 | Conectividad intermitente | Alta | Alto | Polly + colas + caché. |
| R-03 | Dominio vacío → riesgo de diseño apresurado | Alta | Alto | Modelado en Sprint 0/1 con revisión. |
| R-04 | JWT secret hardcodeado (riesgo de seguridad) | Alta | Alto | User Secrets antes de R1. |
| R-05 | Sin tests ni CI → regresiones silenciosas | Alta | Medio | CI básico en R1; cobertura completa en R4. |
| R-06 | Alcance frontend Blazor ambiguo | Media | Medio | Definir en US-XX de R3. |

---

## 12. Métricas de Éxito

**Técnicas:** 0 vulnerabilidades críticas, 0 warnings en build, cobertura ≥ 70%, CI verde, JWT secret fuera del repo.

**Producto:** 100% endpoints documentados, 0 sobreventas, 1 pasarela end-to-end, frontend con CRUD usable.

**Negocio:** ≥ 10 MiPyMES activas al 3er mes, ≥ 500 pedidos procesados el 1er trimestre.

---

## 13. Roadmap de Alto Nivel

| Release | Nombre | Entregable principal | Estado |
|---------|--------|----------------------|--------|
| R0 | Hardening Inicial | JWT secret en User Secrets, tests básicos, CI mínimo, `PermissionProvider` | 🚧 Siguiente |
| R1 | Núcleo de Negocio | Dominio modelado + CRUD productos/categorías + inventario + Swagger | ⏳ Pendiente |
| R2 | Auth Completa | Permisos funcionales, usuarios, refresh tokens, seeder | ⏳ Pendiente |
| R3 | Ventas y Pagos | Carrito + pedidos + pasarelas + webhooks + frontend Blazor admin | ⏳ Pendiente |
| R4 | Refinamiento | Reportes + notificaciones + tests + CI/CD + Docker + hardening | ⏳ Pendiente |

---

## 14. Anexos

- **Anexo A:** Diagrama de arquitectura (README).
- **Anexo B:** Contrato OpenAPI en `/swagger`.
- **Anexo C:** Backlog detallado (docs/BACKLOG.md).
- **Anexo D:** Convenciones de código (README).
