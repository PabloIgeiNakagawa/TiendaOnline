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

(Agregá aquí capturas de pantalla o GIFs del sistema funcionando: tienda, carrito, panel admin, reportes, etc.)

---

## 📌 Estado del proyecto

- ✅ Proyecto funcional en estado avanzado  
- 🚧 Algunas funcionalidades en mejora o expansión  

---

## 👨‍💻 Autor

**Pablo Igei Nakagawa**  
📧 Email: [pabloigeinaka@gmail.com](mailto:pabloigeinaka@gmail.com)  
🔗 LinkedIn: [linkedin.com/in/pablo-igei-nakagawa-4aaa06367](https://www.linkedin.com/in/pablo-igei-nakagawa-4aaa06367)
