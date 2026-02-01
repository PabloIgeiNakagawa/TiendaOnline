# 🖥️ Sistema de Tienda de Componentes de PC

Este es un sistema robusto de gestión para una tienda de hardware, desarrollado bajo el patrón **Arquitectura Limpia** con **ASP.NET Core 8**.
---

## 🚀 Deploy & Acceso
Probá la aplicación en vivo: [http://techstore.somee.com](http://techstore.somee.com)

### 👤 Cuentas de prueba
| Rol | Email | Password |
| :--- | :--- | :--- |
| **Administrador** | admin@gmail.com | 1234 |
| **Repartidor** | repartidor@gmail.com | 1234 |
| **Cliente** | usuario@gmail.com | 1234 |

---

## 🛠️ Stack Tecnológico
* **Backend:** ASP.NET Core 8.
* **Base de Datos:** SQL Server + Entity Framework Core (Code First).
* **Frontend:** JavaScript, HTML5, CSS3, Bootstrap 5.3.3.

---

## ⚙️ Arquitectura y Calidad de Código
Para asegurar la escalabilidad y mantenibilidad, el proyecto implementa:
* **Service Layer Pattern:** Toda la lógica de negocio reside en servicios inyectados, manteniendo los controladores delgados (*Thin Controllers*).
* **Arquitectura:** Clean Architecture (Domain, Data, Services, Infrastructure, Web).

---

## 🎯 Funcionalidades Principales

### 👤 Seguridad y Roles
* Autenticación basada en Cookies y Roles (RBAC).
* Seguridad mediante hashing de contraseñas.
* Gestión de perfiles y auditoría de acciones de usuario.

### 🛍️ E-Commerce & Carrito
* Carrito de compras persistido en sesión con serialización JSON.
* Proceso de checkout con generación automática de pedidos.

### 📦 Administración y Reportes
* **Dashboard:** Visualización de métricas clave mediante gráficos dinámicos.
* **Auditoría:** Registro de logs para trazabilidad de movimientos sensibles.
* **Reportes:** Motor de exportación de datos a formatos **Excel** y **PDF**.
* **Panel de administración:** Gestión de productos, pedidos, usuarios y categorías.

### 🎨 Interfaz y UX
* **Modo Oscuro / Claro:** Implementado mediante Bootstrap y persistencia en Local Storage.

* **Diseño Responsive:** Adaptabilidad total para móviles, tablets y escritorio utilizando Bootstrap.

---

## 🖼️ Galería del Sistema

<details>
<summary>📸 Ver Capturas de Pantalla (Click para expandir)</summary>
### Inicio
<img alt="Inicio" src="https://github.com/user-attachments/assets/a267d4f3-8caa-46da-bab0-5793e3ba5f05" style="max-width: 100%;" />
<img alt="Inicio 2" src="https://github.com/user-attachments/assets/126a0697-ef6b-488e-a08e-ba920f3a37cf" style="max-width: 100%;" />
<img alt="Inicio 3" src="https://github.com/user-attachments/assets/772c6c3a-dfe4-4497-ae75-787e2212fff1" style="max-width: 100%;" />

## Login
<img alt="Login" src="https://github.com/user-attachments/assets/9de55ddc-162f-4484-9dc1-27ced42ee388" style="max-width: 100%;" />

## Productos
<img alt="Productos" src="https://github.com/user-attachments/assets/d6fbd8e9-3563-4e43-b91c-b8b6147bf107" style="max-width: 100%;" />

## Carrito
<img alt="Carrito" src="https://github.com/user-attachments/assets/0cdda37c-84ca-4019-93e3-6902cdbf62a8" style="max-width: 100%;"/>

## Pedido
<img alt="Pedido" src="https://github.com/user-attachments/assets/680e1daf-efe5-4ff3-aeb7-a44cdbb49e90" style="max-width: 100%;" />
<img alt="Pedido 2" src="https://github.com/user-attachments/assets/36abea9a-273c-48d0-b8a8-99cadb2021c1" style="max-width: 100%;" />

Mis pedidos
<img alt="Mis pedidos" src="https://github.com/user-attachments/assets/4290bd0a-fd62-4122-afbd-68aa6d20b975" style="max-width: 100%;" />

Seguimiento pedido (paso a paso)
<img alt="Seguimiento pedido" src="https://github.com/user-attachments/assets/3eed960e-ab09-4215-aa0d-490fb822d4cd" style="max-width: 100%;" />
<img alt="Seguimiento pedido 2" src="https://github.com/user-attachments/assets/76b2df51-5f4e-434e-9d4f-015fb6bec8ba" style="max-width: 100%;" />
<img alt="Seguimiento pedido 3" src="https://github.com/user-attachments/assets/4c93b016-cff4-4029-98a2-7b94d40ae807" style="max-width: 100%;" />
<img alt="Seguimiento pedido 4" src="https://github.com/user-attachments/assets/995e9733-b9bb-4bc1-b69b-ba0665f1c7c5" style="max-width: 100%;" />
<img alt="Seguimiento pedido 5" src="https://github.com/user-attachments/assets/2e0fcf3f-f64e-48c3-aa9b-9c6d14ff7877" style="max-width: 100%;" />


### Panel de administrador
Inicio
<img alt="Inicio admin" src="https://github.com/user-attachments/assets/3ccfa318-af45-4903-8969-36ba964da382" style="max-width: 100%;" />

Dashboards
<img alt="Dashboards" src="https://github.com/user-attachments/assets/0943b4f9-44bc-4158-9373-7849ff2d04e4" style="max-width: 100%;" />
<img alt="Dashboards 2" src="https://github.com/user-attachments/assets/3b5e7cdc-36a0-47ca-a3bd-2c74bb1101db" style="max-width: 100%;" />

Gestión de productos
<img alt="Gestión de productos" src="https://github.com/user-attachments/assets/65d7d76d-a319-4114-b52d-af8d072b3a6d" style="max-width: 100%;" />

Agregar producto
<imga alt="Agregar producto" src="https://github.com/user-attachments/assets/a1bdd4a7-37b5-4d51-8b96-1e61965a3041" style="max-width: 100%;" />
<img alt="Agregar producto 2" src="https://github.com/user-attachments/assets/5111cde9-6a33-4544-8b7e-a5e299efd39f" style="max-width: 100%;" />

Gestión de categorías
<img alt="Gestión de categorías" src="https://github.com/user-attachments/assets/176b9a2a-1e1f-4b55-bfff-1cb02e901e78" style="max-width: 100%;" />

Nueva categoría
<img alt="Nueva categoría" src="https://github.com/user-attachments/assets/f8d5e5e7-51b0-4b54-9843-1ed84dfab374" style="max-width: 100%;" />

Gestión de usuarios
<img alt="Gestión de usuarios" src="https://github.com/user-attachments/assets/2a91f01b-eda6-4e23-9d4e-b7a35871f351" style="max-width: 100%;" />

Crear usuario
<img alt="Crear usuario" src="https://github.com/user-attachments/assets/5f407949-6a73-47c1-9818-5589e98d07c5" style="max-width: 100%;" />

Gestión de pedidos
<img alt="Gestión de pedidos" src="https://github.com/user-attachments/assets/2efded69-217b-4e18-8444-a117300dcc8d" style="max-width: 100%;" />

Auditoría
<img alt="Auditoría" src="https://github.com/user-attachments/assets/ee84d40f-587f-4c62-955b-e4185646d318" style="max-width: 100%;" />


</details>

### 🗺️ Modelo de Datos (DER)
<img alt="DER" src="https://github.com/user-attachments/assets/b0c1d18a-90a2-467f-ac41-a36051cad8af" style="max-width: 100%;" />

---

## 👤 Autor

**Pablo Igei Nakagawa** 📧 Email: [pabloigeinaka@gmail.com](mailto:pabloigeinaka@gmail.com)  
🔗 LinkedIn: [pablo-igei-nakagawa](https://www.linkedin.com/in/pablo-igei-nakagawa-4aaa06367)
