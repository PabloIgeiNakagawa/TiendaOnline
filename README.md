# 🖥️ Sistema de Tienda de Componentes de PC

Este es un sistema robusto de gestión para una tienda de hardware, desarrollado bajo el patrón **MVC** con **ASP.NET Core 8**. El proyecto destaca por su arquitectura desacoplada, implementación de **CI/CD** y una amplia cobertu de **Pruebas Unitarias**.

[GitHub Actions - CI](https://github.com/PabloIgeiNakagawa/TiendaOnline/actions)

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
* **Backend:** ASP.NET Core 8 (MVC)
* **Base de Datos:** SQL Server + Entity Framework Core (Code First)
* **Frontend:** JavaScript (ES6+), HTML5, CSS3, Bootstrap 5
* **Testing:** xUnit, Moq
* **DevOps:** GitHub Actions (CI/CD)

---

## ⚙️ Arquitectura y Calidad de Código
Para asegurar la escalabilidad y mantenibilidad, el proyecto implementa:
* **Service Layer Pattern:** Toda la lógica de negocio reside en servicios inyectados, manteniendo los controladores delgados (*Thin Controllers*).
* **Dependency Injection:** Uso del contenedor nativo de .NET para desacoplar componentes y facilitar el testing.
* **Integración Continua (CI):** Pipeline automatizado en GitHub Actions que compila el proyecto y ejecuta los tests ante cada Push o Pull Request, asegurando la integridad del código en la rama principal.

---

## 🧪 Testing
Se implementó una amplia cobertura de pruebas unitarias para garantizar la integridad de la lógica de negocio:
* **Servicios:** Validación de lógica de negocio (Cálculos de stock, validación de jerarquía de categorías, gestión de estados de pedidos).
* **Controladores:** Pruebas de flujo de navegación, validación de `ModelState`, manejo de sesiones (`HttpContext`), autenticación mediante `Claims` y persistencia de mensajes en `TempData`.
* **Mocks:** Implementación de **Moq** para aislar las pruebas de la base de datos y servicios externos.

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

---

## 🖼️ Galería del Sistema

<details>
<summary>📸 Ver Capturas de Pantalla (Click para expandir)</summary>

### Inicio y Tienda
<img alt="1 - Inicio" src="https://github.com/user-attachments/assets/90257519-7a18-47bf-8199-087885453731" style="max-width: 100%;" />

### Detalle de Producto
<img alt="2 - Productos" src="https://github.com/user-attachments/assets/1f7aa1f8-2306-42cb-a83c-d81d2c6e476a" style="max-width: 100%;" />

### Carrito de Compras
<img alt="3 - Carrito" src="https://github.com/user-attachments/assets/b09aa475-46db-4004-b9a2-6c07d8c67b39" style="max-width: 100%;" />

### Gestión de Pedidos
<img alt="9 - Gestión pedidos" src="https://github.com/user-attachments/assets/dcaeda15-b21f-46d3-bcce-3f526322767f" style="max-width: 100%;" />

### Dashboard de Reportes
<img alt="10 - Dashboard" src="https://github.com/user-attachments/assets/f04e02f9-93ff-4779-88d4-cdf8a7f4beff" style="max-width: 100%;" />

### Auditoría del Sistema
<img alt="12 - Auditoría" src="https://github.com/user-attachments/assets/7860adfc-31f1-4f01-bd1a-df6b29169bcf" style="max-width: 100%;" />

</details>

### 🗺️ Modelo de Datos (DER)
<img alt="13 - Diagrama de bases de datos" src="https://github.com/user-attachments/assets/960eb233-6f4e-4f99-b468-bd5c11bdf5c7" style="max-width: 100%;" />

---

## 👤 Autor

**Pablo Igei Nakagawa** 📧 Email: [pabloigeinaka@gmail.com](mailto:pabloigeinaka@gmail.com)  
🔗 LinkedIn: [pablo-igei-nakagawa](https://www.linkedin.com/in/pablo-igei-nakagawa-4aaa06367)
