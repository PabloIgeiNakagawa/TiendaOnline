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
            labels: [' Pendientes', ' Enviados', ' Entregados', ' Cancelados'],
            datasets: [{
                data: [
                    datosEstadosPedidos.totalPendientes,
                    datosEstadosPedidos.totalEnviados,
                    datosEstadosPedidos.totalEntregados,
                    datosEstadosPedidos.totalCancelados
                ],
                backgroundColor: [
                    '#ffc107',
                    '#0dcaf0',
                    '#198754',
                    '#dc3545'
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