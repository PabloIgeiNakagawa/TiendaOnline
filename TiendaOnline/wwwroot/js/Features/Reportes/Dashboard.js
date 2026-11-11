// Guard: si Chart.js no llegó (archivo ausente, bloqueo de red), TODO el
// dashboard moría en silencio con un TypeError en su primera línea. El banner
// #graficos-averia avisa y corta acá: las tablas son HTML rendereado por Razor
// y se pintan igual, solo faltan los gráficos.
if (typeof Chart === "undefined") {
    const averia = document.getElementById("graficos-averia");
    if (averia) averia.classList.remove("d-none");
} else {
    // Configuración global de Chart.js (debe vivir tras el guard)
    Chart.defaults.font.family = '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif';
    Chart.defaults.color = '#6c757d';

    // Los datos de cada gráfico viajan como <script type="application/json">
    // junto a su canvas (los arma Razor en el parcial de cada sección). Si el
    // contenedor no llegó (sección caída), no hay tag ni canvas: no se arma nada.
    const leerDatos = (id) => {
        const tag = document.getElementById(id);
        try {
            return tag ? JSON.parse(tag.textContent) : [];
        } catch {
            return [];
        }
    };

    const hayDatos = (datos) => Array.isArray(datos) && datos.length > 0;

    const armarVentasMensuales = () => {
        const canvas = document.getElementById('graficoVentasMensuales');
        if (!canvas || Chart.getChart(canvas)) return;

        const datos = leerDatos('datosVentas');
        if (!hayDatos(datos)) return;

        new Chart(canvas, {
            type: 'line',
            data: {
                labels: datos.map(v => v.nombreMes),
                datasets: [{
                    label: 'Ventas',
                    data: datos.map(v => v.totalVentas),
                    borderColor: '#0d6efd',
                    backgroundColor: 'rgba(13, 110, 253, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4,
                    pointRadius: 4,
                    pointHoverRadius: 6,
                    pointBackgroundColor: '#0d6efd',
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        padding: 12,
                        titleFont: { size: 14, weight: 'bold' },
                        bodyFont: { size: 13 },
                        callbacks: {
                            label: function (context) {
                                return ' Ventas: $' + context.parsed.y.toLocaleString('es-AR', {
                                    minimumFractionDigits: 0,
                                    maximumFractionDigits: 0
                                });
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function (value) {
                                return '$' + value.toLocaleString('es-AR', {
                                    minimumFractionDigits: 0,
                                    maximumFractionDigits: 0
                                });
                            }
                        },
                        grid: {
                            color: 'rgba(0, 0, 0, 0.05)'
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        }
                    }
                }
            }
        });
    };

    const armarEstadosPedidos = () => {
        const canvas = document.getElementById('graficoEstadosPedidos');
        if (!canvas || Chart.getChart(canvas)) return;

        const datos = leerDatos('datosEstados');
        if (!datos || Array.isArray(datos)) return;

        new Chart(canvas, {
            type: 'doughnut',
            data: {
                labels: [' Nuevos', ' En preparacion',' Enviados', ' Entregados', ' Cancelados'],
                datasets: [{
                    data: [
                        datos.totalNuevos || 0,
                        datos.totalEnPreparacion || 0,
                        datos.totalEnviados || 0,
                        datos.totalEntregados || 0,
                        datos.totalCancelados || 0
                    ],
                    backgroundColor: [
                        '#6c757d',      // Nuevo (secondary)
                        '#0dcaf0',      // En Preparación (info)
                        '#0d6efd',      // Enviado (primary)
                        '#198754',      // Entregado (success)
                        '#dc3545'       // Cancelado (danger)
                    ],
                    borderWidth: 1,
                    borderColor: '#fff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            padding: 15,
                            usePointStyle: true,
                            font: {
                                size: 12
                            }
                        }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        padding: 12,
                        callbacks: {
                            label: function (context) {
                                const label = context.label || '';
                                const value = context.parsed || 0;
                                // Sin pedidos, value/total = NaN% en el tooltip: si
                                // el total es 0, el porcentaje es 0.
                                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : '0.0';
                                return label + ': ' + value + ' (' + percentage + '%)';
                            }
                        }
                    }
                }
            }
        });
    };

    const armarMetodoPago = () => {
        const canvas = document.getElementById('graficoMetodoPago');
        if (!canvas || Chart.getChart(canvas)) return;

        const datos = leerDatos('datosMetodoPago');
        if (!hayDatos(datos)) return;

        const coloresMetodoPago = [
            '#0d6efd',  // primary
            '#198754',  // success
            '#0dcaf0',  // info
            '#ffc107',  // warning
            '#6f42c1',  // purple
            '#d63384',  // pink
            '#20c997',  // teal
            '#fd7e14'   // orange
        ];
        new Chart(canvas, {
            type: 'bar',
            data: {
                labels: datos.map(m => m.metodoDePago),
                datasets: [{
                    label: 'Ventas',
                    data: datos.map(m => m.totalVentas),
                    backgroundColor: coloresMetodoPago.slice(0, datos.length),
                    borderWidth: 0,
                    borderRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                indexAxis: 'y',
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        padding: 12,
                        titleFont: { size: 14, weight: 'bold' },
                        bodyFont: { size: 13 },
                        callbacks: {
                            label: function (context) {
                                return ' Ventas: $' + context.parsed.x.toLocaleString('es-AR', {
                                    minimumFractionDigits: 0,
                                    maximumFractionDigits: 0
                                });
                            },
                            afterLabel: function (context) {
                                const item = datos[context.dataIndex];
                                return ' Pedidos: ' + item.cantidadPedidos + ' (' + item.porcentajeDelTotal + '%)';
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        beginAtZero: true,
                        ticks: {
                            callback: function (value) {
                                return '$' + value.toLocaleString('es-AR', {
                                    minimumFractionDigits: 0,
                                    maximumFractionDigits: 0
                                });
                            }
                        },
                        grid: {
                            color: 'rgba(0, 0, 0, 0.05)'
                        }
                    },
                    y: {
                        grid: {
                            display: false
                        }
                    }
                }
            }
        });
    };

    const armarDiaHora = () => {
        const canvas = document.getElementById('graficoDiaHora');
        if (!canvas || Chart.getChart(canvas)) return;

        const datos = leerDatos('datosDiaHora');
        if (!hayDatos(datos)) return;

        const diasOrdenados = datos.sort((a, b) => a.ordenDia - b.ordenDia);
        new Chart(canvas, {
            type: 'bar',
            data: {
                labels: diasOrdenados.map(d => d.diaSemana),
                datasets: [
                    {
                        label: 'Madrugada (00-06)',
                        data: diasOrdenados.map(d => d.madrugada),
                        backgroundColor: '#6f42c1',
                        borderRadius: 4
                    },
                    {
                        label: 'Mañana (06-12)',
                        data: diasOrdenados.map(d => d.manana),
                        backgroundColor: '#0dcaf0',
                        borderRadius: 4
                    },
                    {
                        label: 'Tarde (12-18)',
                        data: diasOrdenados.map(d => d.tarde),
                        backgroundColor: '#ffc107',
                        borderRadius: 4
                    },
                    {
                        label: 'Noche (18-00)',
                        data: diasOrdenados.map(d => d.noche),
                        backgroundColor: '#0d6efd',
                        borderRadius: 4
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            padding: 15,
                            usePointStyle: true,
                            font: { size: 12 }
                        }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        padding: 12,
                        titleFont: { size: 14, weight: 'bold' },
                        bodyFont: { size: 13 },
                        callbacks: {
                            label: function (context) {
                                return ' ' + context.dataset.label + ': ' + context.parsed.y + ' pedidos';
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        stacked: true,
                        grid: { display: false }
                    },
                    y: {
                        stacked: true,
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1
                        },
                        grid: {
                            color: 'rgba(0, 0, 0, 0.05)'
                        }
                    }
                }
            }
        });
    };

    // ARMADO POR PINTADO: el caparazón reparte el HTML por data-seccion (site.js)
    // y cada pintado dispara el armado de los gráficos que llegaron en ese
    // reparto. El guard Chart.getChart evita duplicados si un contenedor se
    // repinta (Reintentar): un canvas nuevo nunca tiene gráfico armado.
    const armarGraficos = () => {
        armarVentasMensuales();
        armarEstadosPedidos();
        armarMetodoPago();
        armarDiaHora();
    };

    document.addEventListener('fragmento:pintado', armarGraficos);
    // Cinturón y tirantes: si el reparto corrió antes de este script, el
    // pintado ya pasó — el armado directo lo recupera.
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', armarGraficos);
    } else {
        armarGraficos();
    }
}

// Relojito vivo: #ultimaActualizacion vive en el header estático del caparazón
// (antes el intervalo tiraba TypeError cada 60s buscando un elemento que ya no
// estaba).
setInterval(() => {
    const reloj = document.getElementById('ultimaActualizacion');
    if (!reloj) return;
    const fecha = new Date();
    const opciones = {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    };
    reloj.textContent = fecha.toLocaleString('es-AR', opciones);
}, 60000);
