window.stockSightCharts = (() => {
    const charts = new Map();

    function toTime(value) {
        if (!value) {
            return undefined;
        }

        return Math.floor(new Date(value).getTime() / 1000);
    }

    function createChart(elementId) {
        const element = document.getElementById(elementId);
        if (!element || !window.LightweightCharts) {
            return;
        }

        disposeChart(elementId);

        const chart = LightweightCharts.createChart(element, {
            layout: {
                background: { color: '#111827' },
                textColor: '#cbd5e1'
            },
            grid: {
                vertLines: { color: '#1f2937' },
                horzLines: { color: '#1f2937' }
            },
            rightPriceScale: { borderColor: '#374151' },
            timeScale: { borderColor: '#374151', timeVisible: true },
            autoSize: true,
            crosshair: { mode: LightweightCharts.CrosshairMode.Normal }
        });

        const candleSeries = chart.addSeries
            ? chart.addSeries(LightweightCharts.CandlestickSeries, {
                upColor: '#22c55e',
                downColor: '#ef4444',
                borderVisible: false,
                wickUpColor: '#22c55e',
                wickDownColor: '#ef4444'
            })
            : chart.addCandlestickSeries({
                upColor: '#22c55e',
                downColor: '#ef4444',
                borderVisible: false,
                wickUpColor: '#22c55e',
                wickDownColor: '#ef4444'
            });

        charts.set(elementId, { chart, candleSeries, lines: new Map() });
    }

    function createIndicatorChart(elementId, minimum, maximum) {
        const element = document.getElementById(elementId);
        if (!element || !window.LightweightCharts) {
            return;
        }

        disposeChart(elementId);

        const chart = LightweightCharts.createChart(element, {
            layout: {
                background: { color: '#111827' },
                textColor: '#cbd5e1'
            },
            grid: {
                vertLines: { color: '#1f2937' },
                horzLines: { color: '#1f2937' }
            },
            rightPriceScale: {
                borderColor: '#374151',
                autoScale: minimum === null || minimum === undefined
            },
            timeScale: { borderColor: '#374151', timeVisible: true },
            autoSize: true,
            crosshair: { mode: LightweightCharts.CrosshairMode.Normal }
        });

        if (minimum !== null && minimum !== undefined && maximum !== null && maximum !== undefined) {
            chart.priceScale('right').applyOptions({
                autoScale: false,
                scaleMargins: { top: 0.1, bottom: 0.1 }
            });
        }

        charts.set(elementId, { chart, lines: new Map() });
    }

    function setCandles(elementId, bars) {
        const entry = charts.get(elementId);
        if (!entry) {
            return;
        }

        const data = (bars || [])
            .map(bar => ({
                time: toTime(bar.timestampUtc),
                open: Number(bar.open),
                high: Number(bar.high),
                low: Number(bar.low),
                close: Number(bar.close)
            }))
            .filter(bar => bar.time);

        entry.candleSeries.setData(data);
        entry.chart.timeScale().fitContent();
    }

    function updateTick(elementId, tick) {
        const entry = charts.get(elementId);
        if (!entry || !tick) {
            return;
        }

        const time = toTime(tick.timestampUtc);
        const price = Number(tick.price);
        entry.candleSeries.update({ time, open: price, high: price, low: price, close: price });
    }

    function setLine(elementId, name, points, color) {
        const entry = charts.get(elementId);
        if (!entry) {
            return;
        }

        removeLine(elementId, name);

        const series = entry.chart.addSeries
            ? entry.chart.addSeries(LightweightCharts.LineSeries, { color, lineWidth: 2, priceLineVisible: false })
            : entry.chart.addLineSeries({ color, lineWidth: 2, priceLineVisible: false });

        const data = (points || [])
            .filter(point => point.value !== null && point.value !== undefined)
            .map(point => ({ time: toTime(point.timestampUtc), value: Number(point.value) }))
            .filter(point => point.time);

        series.setData(data);
        entry.lines.set(name, series);
    }

    function removeLine(elementId, name) {
        const entry = charts.get(elementId);
        const series = entry?.lines.get(name);
        if (!entry || !series) {
            return;
        }

        entry.chart.removeSeries(series);
        entry.lines.delete(name);
    }

    function clearLines(elementId) {
        const entry = charts.get(elementId);
        if (!entry) {
            return;
        }

        for (const name of Array.from(entry.lines.keys())) {
            removeLine(elementId, name);
        }
    }

    function setIndicatorLines(elementId, lines) {
        const entry = charts.get(elementId);
        if (!entry) {
            return;
        }

        clearLines(elementId);

        for (const line of lines || []) {
            const series = entry.chart.addSeries
                ? entry.chart.addSeries(LightweightCharts.LineSeries, {
                    color: line.color,
                    lineWidth: line.width || 2,
                    priceLineVisible: false,
                    lastValueVisible: line.lastValueVisible !== false
                })
                : entry.chart.addLineSeries({
                    color: line.color,
                    lineWidth: line.width || 2,
                    priceLineVisible: false,
                    lastValueVisible: line.lastValueVisible !== false
                });

            const data = (line.points || [])
                .filter(point => point.value !== null && point.value !== undefined)
                .map(point => ({ time: toTime(point.timestampUtc), value: Number(point.value) }))
                .filter(point => point.time);

            series.setData(data);
            entry.lines.set(line.name, series);
        }

        entry.chart.timeScale().fitContent();
    }

    function setIndicatorHistogram(elementId, name, points) {
        const entry = charts.get(elementId);
        if (!entry) {
            return;
        }

        const series = entry.chart.addSeries
            ? entry.chart.addSeries(LightweightCharts.HistogramSeries, {
                priceFormat: { type: 'price', precision: 2, minMove: 0.01 },
                priceLineVisible: false,
                lastValueVisible: false
            })
            : entry.chart.addHistogramSeries({
                priceFormat: { type: 'price', precision: 2, minMove: 0.01 },
                priceLineVisible: false,
                lastValueVisible: false
            });

        const data = (points || [])
            .filter(point => point.value !== null && point.value !== undefined)
            .map(point => ({
                time: toTime(point.timestampUtc),
                value: Number(point.value),
                color: Number(point.value) >= 0 ? 'rgba(34, 197, 94, 0.45)' : 'rgba(239, 68, 68, 0.45)'
            }))
            .filter(point => point.time);

        series.setData(data);
        entry.lines.set(name, series);
    }

    function disposeChart(elementId) {
        const entry = charts.get(elementId);
        if (!entry) {
            return;
        }

        entry.chart.remove();
        charts.delete(elementId);
    }

    return {
        createChart,
        createIndicatorChart,
        setCandles,
        updateTick,
        setLine,
        setIndicatorLines,
        setIndicatorHistogram,
        clearLines,
        disposeChart
    };
})();
