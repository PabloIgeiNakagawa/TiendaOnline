import { Chart } from "@/components/ui/chart"
// Variables globales para los gráficos
const charts = {}
let isAnimating = false

// Configuración de colores
const colors = {
    primary: "#007bff",
    success: "#28a745",
    info: "#17a2b8",
    warning: "#ffc107",
    danger: "#dc3545",
    secondary: "#6c757d",
}

const gradients = {
    blue: ["rgba(0, 123, 255, 0.8)", "rgba(0, 123, 255, 0.2)"],
    green: ["rgba(40, 167, 69, 0.8)", "rgba(40, 167, 69, 0.2)"],
    red: ["rgba(220, 53, 69, 0.8)", "rgba(220, 53, 69, 0.2)"],
    purple: ["rgba(102, 16, 242, 0.8)", "rgba(102, 16, 242, 0.2)"],
    orange: ["rgba(255, 193, 7, 0.8)", "rgba(255, 193, 7, 0.2)"],
}

// Inicializar dashboard
function initializeDashboard() {
    console.log("Inicializando dashboard con datos:", window.dashboardData)
    initializeCharts()
    setupEventListeners()
    setupDateFilters()
    animateMetrics()
}

// Configurar event listeners
function setupEventListeners() {
    // Cambio de período
    const periodoSelect = document.getElementById("periodoSelect")
    if (periodoSelect) {
        periodoSelect.addEventListener("change", function () {
            const periodo = this.value
            actualizarDashboard(periodo)
        })
    }

    // Cambio de tipo de gráfico
    const tipoGrafico = document.getElementById("tipoGrafico")
    if (tipoGrafico) {
        tipoGrafico.addEventListener("change", function () {
            const tipo = this.value
            cambiarTipoTodosGraficos(tipo)
        })
    }

    // Filtros de fecha
    const fechaInicio = document.getElementById("fechaInicio")
    const fechaFin = document.getElementById("fechaFin")

    if (fechaInicio) fechaInicio.addEventListener("change", aplicarFiltroFechas)
    if (fechaFin) fechaFin.addEventListener("change", aplicarFiltroFechas)
}

// Configurar filtros de fecha
function setupDateFilters() {
    const hoy = new Date()
    const hace30Dias = new Date(hoy.getTime() - 30 * 24 * 60 * 60 * 1000)

    const fechaFin = document.getElementById("fechaFin")
    const fechaInicio = document.getElementById("fechaInicio")

    if (fechaFin) fechaFin.value = hoy.toISOString().split("T")[0]
    if (fechaInicio) fechaInicio.value = hace30Dias.toISOString().split("T")[0]
}

// Crear gradiente para gráficos
function createGradient(canvasId, colorArray) {
    const canvas = document.getElementById(canvasId)
    if (!canvas) return colorArray[0]

    const ctx = canvas.getContext("2d")
    const gradient = ctx.createLinearGradient(0, 0, 0, 400)
    gradient.addColorStop(0, colorArray[0])
    gradient.addColorStop(1, colorArray[1])
    return gradient
}

// Inicializar gráficos
function initializeCharts() {
    // Verificar que Chart.js esté disponible
    if (typeof Chart === "undefined") {
        console.error("Chart.js no está cargado")
        return
    }

    // Verificar que los datos estén disponibles
    if (!window.dashboardData) {
        console.error("No hay datos disponibles para los gráficos")
        return
    }

    try {
        // Top Productos
        const chartTopProductos = document.getElementById("chartTopProductos")
        if (chartTopProductos && window.dashboardData.topProductos) {
            charts.topProductos = new Chart(chartTopProductos, {
                type: "bar",
                data: {
                    labels: window.dashboardData.topProductos,
                    datasets: [
                        {
                            label: "Unidades Vendidas",
                            data: window.dashboardData.ventasPorProducto,
                            backgroundColor: createGradient("chartTopProductos", gradients.blue),
                            borderColor: colors.primary,
                            borderWidth: 2,
                            borderRadius: 8,
                            borderSkipped: false,
                        },
                    ],
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            display: false,
                        },
                        tooltip: {
                            backgroundColor: "rgba(0, 0, 0, 0.8)",
                            titleColor: "white",
                            bodyColor: "white",
                            borderColor: colors.primary,
                            borderWidth: 1,
                            cornerRadius: 8,
                            displayColors: false,
                            callbacks: {
                                label: (context) => context.parsed.y + " unidades vendidas",
                            },
                        },
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            grid: {
                                color: "rgba(0, 0, 0, 0.1)",
                            },
                            ticks: {
                                callback: (value) => value.toLocaleString(),
                            },
                        },
                        x: {
                            grid: {
                                display: false,
                            },
                        },
                    },
                    animation: {
                        duration: 1000,
                        easing: "easeInOutQuart",
                    },
                },
            })
            console.log("Gráfico de productos inicializado")
        }

        // Estados de Pedidos
        const chartPedidosEstado = document.getElementById("chartPedidosEstado")
        if (chartPedidosEstado && window.dashboardData.estadosPedido) {
            charts.pedidosEstado = new Chart(chartPedidosEstado, {
                type: "doughnut",
                data: {
                    labels: window.dashboardData.estadosPedido,
                    datasets: [
                        {
                            label: "Pedidos por Estado",
                            data: window.dashboardData.cantidadPorEstado,
                            backgroundColor: [colors.info, colors.success, colors.warning, colors.danger],
                            borderWidth: 3,
                            borderColor: "#fff",
                            hoverBorderWidth: 5,
                        },
                    ],
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: "bottom",
                            labels: {
                                padding: 20,
                                usePointStyle: true,
                                font: {
                                    size: 12,
                                },
                            },
                        },
                        tooltip: {
                            backgroundColor: "rgba(0, 0, 0, 0.8)",
                            titleColor: "white",
                            bodyColor: "white",
                            cornerRadius: 8,
                            callbacks: {
                                label: (context) => {
                                    var total = context.dataset.data.reduce((acc, val) => acc + val, 0)
                                    var percentage = ((context.parsed / total) * 100).toFixed(1)
                                    return context.label + ": " + context.parsed + " (" + percentage + "%)"
                                },
                            },
                        },
                    },
                    animation: {
                        animateRotate: true,
                        duration: 1500,
                    },
                },
            })
            console.log("Gráfico de estados inicializado")
        }

        // Ventas por Categoría
        const chartVentasCategoria = document.getElementById("chartVentasCategoria")
        if (chartVentasCategoria && window.dashboardData.categorias) {
            charts.ventasCategoria = new Chart(chartVentasCategoria, {
                type: "bar",
                data: {
                    labels: window.dashboardData.categorias,
                    datasets: [
                        {
                            label: "Ventas por Categoría",
                            data: window.dashboardData.ventasPorCategoria,
                            backgroundColor: createGradient("chartVentasCategoria", gradients.green),
                            borderColor: colors.success,
                            borderWidth: 2,
                            borderRadius: 8,
                            borderSkipped: false,
                        },
                    ],
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            display: false,
                        },
                        tooltip: {
                            backgroundColor: "rgba(0, 0, 0, 0.8)",
                            titleColor: "white",
                            bodyColor: "white",
                            borderColor: colors.success,
                            borderWidth: 1,
                            cornerRadius: 8,
                            displayColors: false,
                            callbacks: {
                                label: (context) =>
                                    new Intl.NumberFormat("es-CO", {
                                        style: "currency",
                                        currency: "COP",
                                        minimumFractionDigits: 0,
                                    }).format(context.parsed.y),
                            },
                        },
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            grid: {
                                color: "rgba(0, 0, 0, 0.1)",
                            },
                            ticks: {
                                callback: (value) =>
                                    new Intl.NumberFormat("es-CO", {
                                        style: "currency",
                                        currency: "COP",
                                        minimumFractionDigits: 0,
                                    }).format(value),
                            },
                        },
                        x: {
                            grid: {
                                display: false,
                            },
                        },
                    },
                    animation: {
                        duration: 1200,
                        easing: "easeInOutQuart",
                    },
                },
            })
            console.log("Gráfico de categorías inicializado")
        }

        // Top Clientes
        const chartTopClientes = document.getElementById("chartTopClientes")
        if (chartTopClientes && window.dashboardData.topClientes) {
            charts.topClientes = new Chart(chartTopClientes, {
                type: "bar",
                data: {
                    labels: window.dashboardData.topClientes,
                    datasets: [
                        {
                            label: "Pedidos por Cliente",
                            data: window.dashboardData.pedidosPorCliente,
                            backgroundColor: createGradient("chartTopClientes", gradients.purple),
                            borderColor: "#6610f2",
                            borderWidth: 2,
                            borderRadius: 8,
                            borderSkipped: false,
                        },
                    ],
                },
                options: {
                    indexAxis: "y",
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            display: false,
                        },
                        tooltip: {
                            backgroundColor: "rgba(0, 0, 0, 0.8)",
                            titleColor: "white",
                            bodyColor: "white",
                            borderColor: "#6610f2",
                            borderWidth: 1,
                            cornerRadius: 8,
                            displayColors: false,
                            callbacks: {
                                label: (context) => context.parsed.x + " pedidos",
                            },
                        },
                    },
                    scales: {
                        x: {
                            beginAtZero: true,
                            grid: {
                                color: "rgba(0, 0, 0, 0.1)",
                            },
                        },
                        y: {
                            grid: {
                                display: false,
                            },
                        },
                    },
                    animation: {
                        duration: 1000,
                        easing: "easeInOutQuart",
                    },
                },
            })
            console.log("Gráfico de clientes inicializado")
        }

        console.log("Gráficos inicializados:", Object.keys(charts))
    } catch (error) {
        console.error("Error al inicializar gráficos:", error)
    }
}

// Actualizar dashboard
function actualizarDashboard(periodo) {
    mostrarLoading(true)

    fetch(`/Reportes/DatosDashboardJson?periodo=${periodo}`)
        .then((res) => {
            if (!res.ok) throw new Error("Error en la respuesta del servidor")
            return res.json()
        })
        .then((data) => {
            // Actualizar datos globales
            window.dashboardData = data

            // Actualizar gráficos
            actualizarGrafico(charts.topProductos, data.topProductos, data.ventasPorProducto)
            actualizarGrafico(charts.pedidosEstado, data.estadosPedido, data.cantidadPorEstado)
            actualizarGrafico(charts.ventasCategoria, data.categorias, data.ventasPorCategoria)
            actualizarGrafico(charts.topClientes, data.topClientes, data.pedidosPorCliente)

            // Actualizar métricas
            actualizarMetricas(data)

            // Actualizar tabla
            actualizarTablaResumen(data)

            mostrarLoading(false)
            mostrarNotificacion("Dashboard actualizado correctamente", "success")
        })
        .catch((error) => {
            console.error("Error:", error)
            mostrarLoading(false)
            mostrarNotificacion("Error al actualizar el dashboard", "error")
        })
}

// Actualizar gráfico
function actualizarGrafico(chart, labels, data) {
    if (chart && labels && data) {
        chart.data.labels = labels
        chart.data.datasets[0].data = data
        chart.update("active")
    }
}

// Actualizar métricas
function actualizarMetricas(data) {
    // Calcular métricas derivadas
    var totalVentas = 0
    var totalPedidos = 0
    var totalProductosVendidos = 0

    if (data.ventasPorCategoria && data.ventasPorCategoria.length) {
        for (let i = 0; i < data.ventasPorCategoria.length; i++) {
            totalVentas += data.ventasPorCategoria[i]
        }
    }

    if (data.cantidadPorEstado && data.cantidadPorEstado.length) {
        for (let i = 0; i < data.cantidadPorEstado.length; i++) {
            totalPedidos += data.cantidadPorEstado[i]
        }
    }

    if (data.ventasPorProducto && data.ventasPorProducto.length) {
        for (let i = 0; i < data.ventasPorProducto.length; i++) {
            totalProductosVendidos += data.ventasPorProducto[i]
        }
    }

    // Animar cambios en las métricas
    animateValue("totalVentas", totalVentas, (value) =>
        new Intl.NumberFormat("es-CO", {
            style: "currency",
            currency: "COP",
            minimumFractionDigits: 0,
        }).format(value),
    )

    animateValue("totalPedidos", totalPedidos)
    animateValue("totalProductosVendidos", totalProductosVendidos)
    animateValue("pedidosCancelados", data.cantidadCancelados)

    // Actualizar porcentajes
    var cambioCancelados = document.getElementById("cambioCancelados")
    if (cambioCancelados) {
        cambioCancelados.innerHTML = '<i class="bi bi-arrow-down"></i> ' + data.porcentajeCancelados.toFixed(1) + "%"
    }
}

// Animar valor numérico
function animateValue(elementId, endValue, formatter) {
    var element = document.getElementById(elementId)
    if (!element) return

    var startValue = 0
    var duration = 1000
    var startTime = performance.now()

    function updateValue(currentTime) {
        var elapsed = currentTime - startTime
        var progress = Math.min(elapsed / duration, 1)
        var currentValue = startValue + (endValue - startValue) * easeOutQuart(progress)

        if (formatter) {
            element.textContent = formatter(Math.round(currentValue))
        } else {
            element.textContent = Math.round(currentValue).toLocaleString()
        }

        if (progress < 1) {
            requestAnimationFrame(updateValue)
        }
    }

    requestAnimationFrame(updateValue)
}

// Función de easing
function easeOutQuart(t) {
    return 1 - Math.pow(1 - t, 4)
}

// Animar métricas al cargar
function animateMetrics() {
    var metricCards = document.querySelectorAll(".metric-card")
    for (var i = 0; i < metricCards.length; i++) {
        ; ((index) => {
            setTimeout(() => {
                metricCards[index].classList.add("fade-in-up")
            }, index * 100)
        })(i)
    }
}

// Cambiar tipo de gráfico
function cambiarTipoGrafico(chartId, tipo) {
    var chartKey = chartId.replace("chart", "").toLowerCase()
    if (charts[chartKey]) {
        charts[chartKey].config.type = tipo
        charts[chartKey].update()
    }
}

// Cambiar tipo de todos los gráficos
function cambiarTipoTodosGraficos(tipo) {
    for (var key in charts) {
        if (charts.hasOwnProperty(key)) {
            if (tipo !== "doughnut" || key === "pedidosestado") {
                charts[key].config.type = tipo === "doughnut" && key !== "pedidosestado" ? "pie" : tipo
                charts[key].update()
            }
        }
    }
}

// Toggle animación
function toggleAnimacion(chartId) {
    var chartKey = chartId.replace("chart", "").toLowerCase()
    if (charts[chartKey] && !isAnimating) {
        isAnimating = true
        charts[chartKey].update("active")
        setTimeout(() => {
            isAnimating = false
        }, 1000)
    }
}

// Aplicar filtro de fechas
function aplicarFiltroFechas() {
    var fechaInicio = document.getElementById("fechaInicio")
    var fechaFin = document.getElementById("fechaFin")

    if (fechaInicio && fechaFin && fechaInicio.value && fechaFin.value) {
        mostrarLoading(true)

        fetch("/Reportes/DatosDashboardJson?fechaInicio=" + fechaInicio.value + "&fechaFin=" + fechaFin.value)
            .then((res) => res.json())
            .then((data) => {
                window.dashboardData = data
                actualizarMetricas(data)
                actualizarGrafico(charts.topProductos, data.topProductos, data.ventasPorProducto)
                actualizarGrafico(charts.pedidosEstado, data.estadosPedido, data.cantidadPorEstado)
                actualizarGrafico(charts.ventasCategoria, data.categorias, data.ventasPorCategoria)
                actualizarGrafico(charts.topClientes, data.topClientes, data.pedidosPorCliente)
                actualizarTablaResumen(data)
                mostrarLoading(false)
            })
            .catch((error) => {
                console.error("Error:", error)
                mostrarLoading(false)
            })
    }
}

// Actualizar datos
function actualizarDatos() {
    var periodoSelect = document.getElementById("periodoSelect")
    if (periodoSelect) {
        var periodo = periodoSelect.value
        actualizarDashboard(periodo)
    }
}

// Exportar reporte
function exportarReporte() {
    var boton = document.querySelector('[onclick="exportarReporte()"]')
    if (!boton) return

    var textoOriginal = boton.innerHTML

    boton.innerHTML = '<i class="bi bi-arrow-repeat spin me-2"></i>Exportando...'
    boton.disabled = true

    // Simular exportación
    setTimeout(() => {
        boton.innerHTML = '<i class="bi bi-check-circle me-2"></i>Exportado'
        boton.classList.add("btn-success")
        boton.classList.remove("btn-outline-primary")

        setTimeout(() => {
            boton.innerHTML = textoOriginal
            boton.classList.remove("btn-success")
            boton.classList.add("btn-outline-primary")
            boton.disabled = false
        }, 2000)
    }, 1500)
}

// Mostrar/ocultar loading
function mostrarLoading(show) {
    var overlay = document.getElementById("loadingOverlay")
    if (overlay) {
        overlay.style.display = show ? "flex" : "none"
    }
}

// Mostrar notificación
function mostrarNotificacion(mensaje, tipo) {
    tipo = tipo || "info"

    // Verificar que Bootstrap esté disponible
    if (typeof bootstrap === "undefined") {
        console.log(mensaje)
        return
    }

    // Crear toast de notificación
    var toast = document.createElement("div")
    toast.className =
        "toast align-items-center text-white bg-" + (tipo === "success" ? "success" : "danger") + " border-0 position-fixed"
    toast.style.cssText = "top: 20px; right: 20px; z-index: 1055; min-width: 300px;"
    toast.setAttribute("role", "alert")
    toast.innerHTML =
        '<div class="d-flex">' +
        '<div class="toast-body">' +
        '<i class="bi bi-' +
        (tipo === "success" ? "check-circle" : "exclamation-triangle") +
        ' me-2"></i>' +
        mensaje +
        "</div>" +
        '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>' +
        "</div>"

    document.body.appendChild(toast)
    try {
        var bsToast = new bootstrap.Toast(toast, { delay: 3000 })
        bsToast.show()

        toast.addEventListener("hidden.bs.toast", () => {
            toast.remove()
        })
    } catch (error) {
        console.error("Bootstrap Toast error:", error)
        console.log(mensaje)
    }
}

// Actualizar tabla de resumen
function actualizarTablaResumen(data) {
    var totalVentas = 0
    var totalPedidos = 0

    if (data.ventasPorCategoria && data.ventasPorCategoria.length) {
        for (let i = 0; i < data.ventasPorCategoria.length; i++) {
            totalVentas += data.ventasPorCategoria[i]
        }
    }

    if (data.cantidadPorEstado && data.cantidadPorEstado.length) {
        for (let i = 0; i < data.cantidadPorEstado.length; i++) {
            totalPedidos += data.cantidadPorEstado[i]
        }
    }

    var ventasActuales = document.getElementById("ventasActuales")
    if (ventasActuales) {
        ventasActuales.textContent = new Intl.NumberFormat("es-CO", {
            style: "currency",
            currency: "COP",
            minimumFractionDigits: 0,
        }).format(totalVentas)
    }

    var pedidosCompletados = document.getElementById("pedidosCompletados")
    if (pedidosCompletados) {
        pedidosCompletados.textContent = (totalPedidos - data.cantidadCancelados).toLocaleString()
    }

    var tasaCancelacion = document.getElementById("tasaCancelacion")
    if (tasaCancelacion) {
        tasaCancelacion.textContent = data.porcentajeCancelados.toFixed(1) + "%"
    }

    var ticketPromedio = document.getElementById("ticketPromedio")
    if (ticketPromedio) {
        ticketPromedio.textContent = new Intl.NumberFormat("es-CO", {
            style: "currency",
            currency: "COP",
            minimumFractionDigits: 0,
        }).format(totalVentas / Math.max(totalPedidos, 1))
    }
}

// Hacer las funciones globales para que puedan ser llamadas desde HTML
window.actualizarDatos = actualizarDatos
window.exportarReporte = exportarReporte
window.cambiarTipoGrafico = cambiarTipoGrafico
window.toggleAnimacion = toggleAnimacion
window.initializeDashboard = initializeDashboard
