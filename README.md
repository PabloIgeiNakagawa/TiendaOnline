# 🖥️ Sistema de Tienda de Componentes de PC

Este es un sistema completo de gestión para una tienda de componentes de computadoras, desarrollado con **.NET**, **SQL Server** y **Bootstrap**. Incluye funcionalidades tanto para el usuario final (tienda online) como para el administrador (gestión de productos, pedidos y reportes).

---

## 🚀 Tecnologías utilizadas

- **Backend:** ASP.NET Core MVC  
- **Frontend:** HTML5, CSS3, JavaScript, Bootstrap  
- **Base de datos:** SQL Server  
- **ORM:** Entity Framework Core  

---

## 🎯 Funcionalidades principales

### 👥 Roles

- Usuario  
- Administrador  
- Repartidor  

### 👤 Usuarios

- Registro y login  
- Visualización de productos  
- Agregar productos al carrito  
- Confirmar pedidos  
- Historial de compras  

### 🛍️ Carrito de compras

- Agregar, quitar y modificar productos  
- Cálculo automático del total  

### 📦 Administración

- Gestión de productos, usuarios y pedidos (Alta, baja, modificación)  
- Auditoría del sistema (logs y registros de acciones)  

### 📈 Reportes

- Productos más vendidos  
- Ventas por categoría  
- Clientes con más pedidos  
- Estado de todos los pedidos  

### 🧾 Pedidos

- Visualización y estado del pedido (pendiente, enviado, entregado)  
- Detalle completo de cada pedido  

---

## 💡 Extras

- Seguridad con hash de contraseñas  
- Validaciones del lado del cliente y del servidor  
- Exportación de reportes a **Excel** o **PDF**  

---

## 🛠️ Instalación y ejecución

### Requisitos previos

- .NET SDK 6 o 7  
- SQL Server (Express o full)  
- Visual Studio 2022 o superior  

### Pasos

1. Clonar el repositorio:

   ```bash
   git clone https://github.com/PabloIgeiNakagawa/TiendaOnline.git
   ```

2. Abrir la solución `.sln` en Visual Studio

3. Configurar la cadena de conexión en `appsettings.json`:

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=TU_SERVIDOR;Database=TiendaPC;Trusted_Connection=True;"
   }
   ```

4. Ejecutar las migraciones:

   ```bash
   dotnet ef database update
   ```

5. Ejecutar el proyecto desde Visual Studio (`F5` o `Ctrl + F5`)

---

## 🖼️ Capturas
### Inicio
<img alt="1 - Inicio" src="https://github.com/user-attachments/assets/90257519-7a18-47bf-8199-087885453731" style="max-width: 100%; height: auto;" />
## Producto
<img alt="2 - Productos" src="https://github.com/user-attachments/assets/1f7aa1f8-2306-42cb-a83c-d81d2c6e476a" style="max-width: 100%; height: auto;" />
## Carrito
<img alt="3 - carrito" src="https://github.com/user-attachments/assets/b09aa475-46db-4004-b9a2-6c07d8c67b39" style="max-width: 100%; height: auto;" />
## Pedido
<img alt="4 - Pedido" src="https://github.com/user-attachments/assets/108bfbb3-b0c1-49ab-9bd9-f29f3ded528e" style="max-width: 100%; height: auto;" />
<img alt="5 - Pedido hecho" src="https://github.com/user-attachments/assets/7b36dbc4-7c9e-4123-ad82-9b7e4f43b08f" style="max-width: 100%; height: auto;" />
## Paneles de admin
<img alt="6 - Panel de admin" src="https://github.com/user-attachments/assets/d37bfabc-1b63-487d-a295-96c379a561f9" style="max-width: 100%; height: auto;" />
<img alt="7 - gestion productos" src="https://github.com/user-attachments/assets/27892928-d157-4c47-8e86-21ea768a0b3b" style="max-width: 100%; height: auto;" />
<img alt="8 - gestion usuarios" src="https://github.com/user-attachments/assets/a0fd3b5a-ae6b-4604-acc3-66acda66faaa" style="max-width: 100%; height: auto;" />
<img alt="9 - gestion pedidos" src="https://github.com/user-attachments/assets/dcaeda15-b21f-46d3-bcce-3f526322767f" style="max-width: 100%; height: auto;" />
<img alt="10 - dashboard" src="https://github.com/user-attachments/assets/f04e02f9-93ff-4779-88d4-cdf8a7f4beff" style="max-width: 100%; height: auto;" />
<img alt="11 - dashboard2" src="https://github.com/user-attachments/assets/6e400483-30b7-4184-8de5-c8c15b1aa0e6" style="max-width: 100%; height: auto;" />
<img alt="12 - auditoria" src="https://github.com/user-attachments/assets/7860adfc-31f1-4f01-bd1a-df6b29169bcf" style="max-width: 100%; height: auto;" />

---

## 👤 Autor

**Pablo Igei Nakagawa**  
📧 Email: [pabloigeinaka@gmail.com](mailto:pabloigeinaka@gmail.com)  
🔗 LinkedIn: [linkedin.com/in/pablo-igei-nakagawa-4aaa06367](https://www.linkedin.com/in/pablo-igei-nakagawa-4aaa06367)
