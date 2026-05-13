window.spcInterop = (function () {
    const charts = {};

    function destroyChart(id) {
        if (charts[id]) { charts[id].destroy(); delete charts[id]; }
    }

    function makeConstantDataset(length, value, label, color, dash) {
        return {
            label,
            data: Array(length).fill(value),
            borderColor: color,
            borderDash: dash || [],
            borderWidth: 1.5,
            pointRadius: 0,
            fill: false,
            tension: 0,
        };
    }

    function violationPlugin(violations, color) {
        return {
            id: 'violationHighlight',
            afterDraw(chart) {
                const ctx = chart.ctx;
                violations.forEach(idx => {
                    const meta = chart.getDatasetMeta(0);
                    if (!meta.data[idx]) return;
                    const pt = meta.data[idx];
                    ctx.save();
                    ctx.beginPath();
                    ctx.arc(pt.x, pt.y, 8, 0, 2 * Math.PI);
                    ctx.strokeStyle = color || '#ff0000';
                    ctx.lineWidth = 2;
                    ctx.stroke();
                    ctx.restore();
                });
            }
        };
    }

    return {
        createXBarChart(canvasId, data) {
            destroyChart(canvasId);
            const el = document.getElementById(canvasId);
            if (!el) return;
            const n = data.labels.length;
            const title = data.componentCode
                ? `X-bar Chart — ${data.componentCode}${data.dimensionName ? ' / ' + data.dimensionName : ''}`
                : 'X-bar Chart';

            charts[canvasId] = new Chart(el.getContext('2d'), {
                type: 'line',
                data: {
                    labels: data.labels,
                    datasets: [
                        {
                            label: 'X-bar (Mean)',
                            data: data.values,
                            borderColor: '#0d6efd',
                            backgroundColor: 'rgba(13,110,253,0.08)',
                            pointRadius: 4,
                            pointHoverRadius: 6,
                            borderWidth: 2,
                            fill: false,
                            tension: 0.1,
                            pointBackgroundColor: data.values.map((_, i) =>
                                data.violations.includes(i) ? '#dc3545' : '#0d6efd'),
                        },
                        makeConstantDataset(n, data.ucl, `UCL (${data.ucl})`, '#dc3545', [8, 4]),
                        makeConstantDataset(n, data.center, `CL (${data.center})`, '#198754', [5, 3]),
                        makeConstantDataset(n, data.lcl, `LCL (${data.lcl})`, '#dc3545', [8, 4]),
                        makeConstantDataset(n, data.usl, `USL (${data.usl})`, '#fd7e14', [3, 3]),
                        makeConstantDataset(n, data.lsl, `LSL (${data.lsl})`, '#fd7e14', [3, 3]),
                    ],
                },
                options: {
                    responsive: true,
                    animation: false,
                    plugins: {
                        title: { display: true, text: title, font: { size: 13 } },
                        legend: { position: 'bottom', labels: { boxWidth: 20, font: { size: 11 } } },
                        tooltip: {
                            callbacks: {
                                label: ctx => `${ctx.dataset.label}: ${ctx.parsed.y}`
                            }
                        }
                    },
                    scales: {
                        x: { ticks: { maxRotation: 45 } },
                        y: { title: { display: true, text: 'Value' } }
                    }
                },
                plugins: [violationPlugin(data.violations)]
            });
        },

        createRChart(canvasId, data) {
            destroyChart(canvasId);
            const el = document.getElementById(canvasId);
            if (!el) return;
            const n = data.labels.length;

            charts[canvasId] = new Chart(el.getContext('2d'), {
                type: 'line',
                data: {
                    labels: data.labels,
                    datasets: [
                        {
                            label: 'Range (R)',
                            data: data.values,
                            borderColor: '#6f42c1',
                            backgroundColor: 'rgba(111,66,193,0.08)',
                            pointRadius: 4,
                            pointHoverRadius: 6,
                            borderWidth: 2,
                            fill: false,
                            tension: 0.1,
                            pointBackgroundColor: data.values.map((_, i) =>
                                data.violations.includes(i) ? '#dc3545' : '#6f42c1'),
                        },
                        makeConstantDataset(n, data.ucl, `UCL (${data.ucl})`, '#dc3545', [8, 4]),
                        makeConstantDataset(n, data.rBar, `R-bar (${data.rBar})`, '#198754', [5, 3]),
                        ...(data.lcl > 0 ? [makeConstantDataset(n, data.lcl, `LCL (${data.lcl})`, '#dc3545', [8, 4])] : []),
                    ],
                },
                options: {
                    responsive: true,
                    animation: false,
                    plugins: {
                        title: { display: true, text: 'R Chart (Range)', font: { size: 13 } },
                        legend: { position: 'bottom', labels: { boxWidth: 20, font: { size: 11 } } },
                    },
                    scales: {
                        x: { ticks: { maxRotation: 45 } },
                        y: { beginAtZero: true, title: { display: true, text: 'Range' } }
                    }
                },
                plugins: [violationPlugin(data.violations)]
            });
        },

        downloadFile(content, filename, mimeType) {
            const blob = new Blob([content], { type: mimeType });
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url; a.download = filename; a.click();
            setTimeout(() => URL.revokeObjectURL(url), 1000);
        },

        downloadBytes(bytes, filename, mimeType) {
            const blob = new Blob([new Uint8Array(bytes)], { type: mimeType });
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url; a.download = filename; a.click();
            setTimeout(() => URL.revokeObjectURL(url), 1000);
        },

        triggerFileInput(id) {
            const el = document.getElementById(id);
            if (el) el.click();
        },

        async exportPdf(filename) {
            const { jsPDF } = window.jspdf;
            const doc = new jsPDF('p', 'mm', 'a4');
            const pageW = doc.internal.pageSize.getWidth();
            const margin = 10;

            const element = document.getElementById('spc-report');
            if (!element) { alert('Report element not found.'); return; }

            const canvas = await html2canvas(element, {
                scale: 1.5,
                useCORS: true,
                allowTaint: true,
                backgroundColor: '#ffffff',
            });

            const imgData = canvas.toDataURL('image/png');
            const imgW = pageW - margin * 2;
            const imgH = (canvas.height / canvas.width) * imgW;
            const pageH = doc.internal.pageSize.getHeight() - margin * 2;

            let yOffset = 0;
            let remaining = imgH;
            let page = 0;

            while (remaining > 0) {
                if (page > 0) doc.addPage();
                const sliceH = Math.min(remaining, pageH);
                const sourceY = yOffset * (canvas.height / imgH);
                const sliceCanvas = document.createElement('canvas');
                sliceCanvas.width = canvas.width;
                sliceCanvas.height = (sliceH / imgH) * canvas.height;
                const sctx = sliceCanvas.getContext('2d');
                sctx.drawImage(canvas, 0, -sourceY);
                doc.addImage(sliceCanvas.toDataURL('image/png'), 'PNG', margin, margin, imgW, sliceH);
                yOffset += sliceH;
                remaining -= sliceH;
                page++;
            }

            doc.save(filename);
        }
    };
})();
