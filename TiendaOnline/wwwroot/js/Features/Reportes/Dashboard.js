// Configuración global de Chart.js
Chart.defaults.font.family = '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif';
Chart.defaults.color = '#6c757d';

document.addEventListener("DOMContentLoaded", function () {
    const datosVentasMensuales = JSON.parse(
        document.getElementById("datosVentas").textContent
    );

    const datosEstadosPedidos = JSON.parse(
        document.getElementById("datosEstados").textContent
    );

    const datosMetodoPago = JSON.parse(
        document.getElementById("datosMetodoPago")?.textContent || "[]"
    );

    const datosDiaHora = JSON.parse(
        document.getElementById("datosDiaHora")?.textContent || "[]"
    );

    // Gráfico de Ventas Mensuales
    const ctxVentas = document.getElementById('graficoVentasMensuales').getContext('2d');
    new Chart(ctxVentas, {
        type: 'line',
        data: {
            labels: datosVentasMensuales.map(v => v.nombreMes),
            datasets: [{
                label: 'Ventas',
                data: datosVentasMensuales.map(v => v.totalVentas),
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

    // Gráfico de Estados de Pedidos
    const ctxEstados = document.getElementById('graficoEstadosPedidos').getContext('2d');
    new Chart(ctxEstados, {
        type: 'doughnut',
        data: {
            labels: [' Nuevos', ' En preparacion',' Enviados', ' Entregados', ' Cancelados'],
            datasets: [{
                data: [
                    datosEstadosPedidos.totalNuevos,
                    datosEstadosPedidos.totalEnPreparacion,
                    datosEstadosPedidos.totalEnviados,
                    datosEstadosPedidos.totalEntregados,
                    datosEstadosPedidos.totalCancelados
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
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const percentage = ((value / total) * 100).toFixed(1);
                            return label + ': ' + value + ' (' + percentage + '%)';
                        }
                    }
                }
            }
        }
    });

    // Gráfico de Ventas por Método de Pago
    if (datosMetodoPago.length > 0) {
        const ctxMetodoPago = document.getElementById('graficoMetodoPago').getContext('2d');
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
        new Chart(ctxMetodoPago, {
            type: 'bar',
            data: {
                labels: datosMetodoPago.map(m => m.metodoDePago),
                datasets: [{
                    label: 'Ventas',
                    data: datosMetodoPago.map(m => m.totalVentas),
                    backgroundColor: coloresMetodoPago.slice(0, datosMetodoPago.length),
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
                                const item = datosMetodoPago[context.dataIndex];
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
    }

    // Gráfico de Pedidos por Día y Hora
    if (datosDiaHora.length > 0) {
        const ctxDiaHora = document.getElementById('graficoDiaHora').getContext('2d');
        const diasOrdenados = datosDiaHora.sort((a, b) => a.ordenDia - b.ordenDia);
        new Chart(ctxDiaHora, {
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
    }


    // Actualizar fecha y hora cada minuto
    setInterval(() => {
        const fecha = new Date();
        const opciones = {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        };
        document.getElementById('ultimaActualizacion').textContent =
            fecha.toLocaleString('es-AR', opciones);
    }, 60000);

});