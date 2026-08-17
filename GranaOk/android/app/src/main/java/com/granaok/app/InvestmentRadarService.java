package com.granaok.app;

import android.app.Service;
import android.content.Intent;
import android.os.Bundle;
import android.os.Handler;
import android.os.IBinder;
import android.os.Looper;
import android.os.Process;
import android.os.ResultReceiver;

import org.json.JSONArray;
import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.URL;
import java.net.URLEncoder;
import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.Locale;
import java.util.concurrent.atomic.AtomicBoolean;

/** Radar informativo com dados públicos oficiais. Não executa ordens nem recomenda produto individual. */
public class InvestmentRadarService extends Service {
    public static final String EXTRA_RECEIVER = "radar_receiver";
    private static final int CODE_RESULT = 2;
    private static final long HARD_TIMEOUT_MS = 24000L;
    private final Handler mainHandler = new Handler(Looper.getMainLooper());

    @Override public IBinder onBind(Intent intent) { return null; }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        if (intent == null) { stopSelf(startId); return START_NOT_STICKY; }
        final ResultReceiver receiver = intent.getParcelableExtra(EXTRA_RECEIVER);
        final AtomicBoolean delivered = new AtomicBoolean(false);
        final Runnable timeout = () -> {
            if (delivered.compareAndSet(false, true)) send(receiver, fail("As fontes online demoraram demais para responder."));
            stopSelf(startId); mainHandler.postDelayed(() -> Process.killProcess(Process.myPid()), 250L);
        };
        mainHandler.postDelayed(timeout, HARD_TIMEOUT_MS);
        Thread worker = new Thread(() -> {
            JSONObject out;
            try { out = buildRadar(); } catch (Throwable e) { out = fail(clean(e)); }
            if (delivered.compareAndSet(false, true)) {
                mainHandler.removeCallbacks(timeout); send(receiver, out); stopSelf(startId);
            }
        }, "GranaOk-Investment-Radar");
        worker.setDaemon(true); worker.start();
        return START_NOT_STICKY;
    }

    private JSONObject buildRadar() throws Exception {
        JSONObject out = new JSONObject(); out.put("ok", true);
        out.put("updated_at", new SimpleDateFormat("yyyy-MM-dd HH:mm", Locale.getDefault()).format(Calendar.getInstance().getTime()));

        JSONObject benchmarks = new JSONObject();
        JSONObject sourceStatus = new JSONObject();
        try {
            JSONObject selic = latestBcb(1178, 60);
            benchmarks.put("selic_effective", selic); sourceStatus.put("bcb_selic", "ok");
        } catch (Throwable e) { sourceStatus.put("bcb_selic", "error: " + shortMsg(e)); }
        try {
            JSONObject meta = latestBcb(432, 90);
            benchmarks.put("selic_target", meta); sourceStatus.put("bcb_meta", "ok");
        } catch (Throwable e) { sourceStatus.put("bcb_meta", "error: " + shortMsg(e)); }
        try {
            JSONObject ipca = latestBcb(433, 550);
            benchmarks.put("ipca_monthly", ipca); sourceStatus.put("bcb_ipca", "ok");
        } catch (Throwable e) { sourceStatus.put("bcb_ipca", "error: " + shortMsg(e)); }
        out.put("benchmarks", benchmarks);

        JSONArray treasury = new JSONArray();
        try {
            treasury = fetchTreasury(); sourceStatus.put("tesouro", treasury.length() > 0 ? "ok" : "sem títulos retornados");
        } catch (Throwable e) { sourceStatus.put("tesouro", "indisponível: " + shortMsg(e)); }
        out.put("treasury", treasury); out.put("source_status", sourceStatus);

        JSONArray radar = new JSONArray();
        double selic = benchmarks.optJSONObject("selic_target") == null ?
            (benchmarks.optJSONObject("selic_effective") == null ? 0d : benchmarks.optJSONObject("selic_effective").optDouble("value", 0d)) :
            benchmarks.optJSONObject("selic_target").optDouble("value", 0d);
        double ipca = benchmarks.optJSONObject("ipca_monthly") == null ? 0d : benchmarks.optJSONObject("ipca_monthly").optDouble("value", 0d);
        radar.put(radarItem("Reserva / curto prazo", "Tesouro Selic ou CDB com liquidez diária", selic > 0 ? "Compare o rendimento líquido com a Selic de referência em " + one(selic) + "% a.a. e confirme liquidez, garantia e carência." : "Priorize liquidez, risco e custo total antes da taxa anunciada.", "liquidity"));
        radar.put(radarItem("Prazo definido", "CDB, LCI/LCA e Tesouro Prefixado", "Compare vencimento, possibilidade de resgate, risco do emissor e retorno líquido. Taxa maior isoladamente não significa melhor opção.", "term"));
        radar.put(radarItem("Proteção contra inflação", "Tesouro IPCA+", ipca != 0 ? "O IPCA mensal mais recente na fonte do BCB é " + one(ipca) + "%. Para longo prazo, compare a taxa real do título e o vencimento." : "Compare taxa real acima do IPCA, vencimento e risco de marcação a mercado se vender antes.", "inflation"));
        radar.put(radarItem("Comparação de renda fixa", "Use CDI/Taxa DI como benchmark", "A Taxa DI é calculada e divulgada pela B3 e serve de referência para CDBs e outros títulos privados. O GranaOk não inventa ofertas de corretoras sem fonte verificável.", "benchmark"));
        out.put("radar", radar);

        JSONArray refs = new JSONArray();
        refs.put(ref("Banco Central · Selic anualizada", "https://dadosabertos.bcb.gov.br/dataset/1178-taxa-de-juros---selic-anualizada-base-252"));
        refs.put(ref("Banco Central · Meta Selic", "https://dadosabertos.bcb.gov.br/dataset/432-taxa-de-juros---meta-selic-definida-pelo-copom"));
        refs.put(ref("Banco Central · IPCA", "https://www3.bcb.gov.br/sgspub/consultarvalores/consultarValoresSeries.do?method=consultarGraficoPorId&hdOidSeriesSelecionadas=433"));
        refs.put(ref("B3 · Depósito Interfinanceiro / Taxa DI", "https://www.b3.com.br/pt_br/produtos-e-servicos/registro/renda-fixa-e-valores-mobiliarios/deposito-interfinanceiro.htm"));
        refs.put(ref("Tesouro Direto · preços e taxas", "https://www.tesourodireto.com.br/produtos/dados-sobre-titulos/historico-de-precos-e-taxas"));
        out.put("references", refs);
        out.put("disclaimer", "Radar informativo. Não é recomendação personalizada nem substitui a análise de risco, liquidez, tributação e objetivo do investidor.");
        return out;
    }

    private JSONObject latestBcb(int series, int daysBack) throws Exception {
        Calendar end = Calendar.getInstance(); Calendar start = (Calendar) end.clone(); start.add(Calendar.DAY_OF_YEAR, -daysBack);
        SimpleDateFormat fmt = new SimpleDateFormat("dd/MM/yyyy", Locale.US);
        String url = "https://api.bcb.gov.br/dados/serie/bcdata.sgs." + series + "/dados?formato=json&dataInicial=" +
            URLEncoder.encode(fmt.format(start.getTime()), "UTF-8") + "&dataFinal=" + URLEncoder.encode(fmt.format(end.getTime()), "UTF-8");
        String body = get(url, "application/json"); JSONArray data = new JSONArray(body);
        if (data.length() == 0) throw new IllegalStateException("série " + series + " sem dados no período");
        JSONObject last = data.getJSONObject(data.length() - 1);
        JSONObject out = new JSONObject(); out.put("series", series); out.put("date", last.optString("data")); out.put("value", parseDouble(last.optString("valor"))); return out;
    }

    private JSONArray fetchTreasury() throws Exception {
        String endpoint = "https://www.tesourodireto.com.br/json/br/com/b3/tesourodireto/service/api/treasurybondsinfo.json";
        String body = get(endpoint, "application/json,text/plain,*/*"); JSONObject root = new JSONObject(body); JSONObject response = root.optJSONObject("response");
        if (response == null) throw new IllegalStateException("formato do Tesouro Direto não reconhecido");
        JSONArray list = response.optJSONArray("TrsrBdTradgList"); JSONArray out = new JSONArray(); if (list == null) return out;
        for (int i = 0; i < list.length() && out.length() < 30; i++) {
            JSONObject wrap = list.optJSONObject(i); JSONObject b = wrap == null ? null : wrap.optJSONObject("TrsrBd"); if (b == null) continue;
            String name = b.optString("nm", "").trim(); if (name.isEmpty()) continue;
            JSONObject x = new JSONObject(); x.put("name", name); x.put("maturity", trimDate(b.optString("mtrtyDt", "")));
            x.put("annual_invest_rate", b.optDouble("anulInvstmtRate", 0d)); x.put("unit_invest_value", b.optDouble("untrInvstmtVal", 0d));
            x.put("annual_redemption_rate", b.optDouble("anulRedRate", 0d)); x.put("unit_redemption_value", b.optDouble("untrRedVal", 0d));
            out.put(x);
        }
        return out;
    }

    private String get(String address, String accept) throws Exception {
        HttpURLConnection c = (HttpURLConnection) new URL(address).openConnection();
        c.setConnectTimeout(7000); c.setReadTimeout(9000); c.setRequestMethod("GET"); c.setInstanceFollowRedirects(true);
        c.setRequestProperty("Accept", accept); c.setRequestProperty("User-Agent", "Mozilla/5.0 (Linux; Android) GranaOk/0.4");
        int code = c.getResponseCode(); InputStream in = code >= 200 && code < 300 ? c.getInputStream() : c.getErrorStream();
        String body = read(in); c.disconnect(); if (code < 200 || code >= 300) throw new IllegalStateException("HTTP " + code); return body;
    }

    private static String read(InputStream in) throws Exception { if (in == null) return ""; BufferedReader r = new BufferedReader(new InputStreamReader(in, "UTF-8")); StringBuilder b = new StringBuilder(); String line; while ((line = r.readLine()) != null && b.length() < 1200000) b.append(line).append('\n'); r.close(); return b.toString(); }
    private static JSONObject radarItem(String objective, String option, String note, String kind) throws Exception { JSONObject o = new JSONObject(); o.put("objective", objective); o.put("option", option); o.put("note", note); o.put("kind", kind); return o; }
    private static JSONObject ref(String name, String url) throws Exception { JSONObject o = new JSONObject(); o.put("name", name); o.put("url", url); return o; }
    private static String trimDate(String s) { return s == null ? "" : (s.length() >= 10 ? s.substring(0, 10) : s); }
    private static double parseDouble(String s) { try { return Double.parseDouble((s == null ? "0" : s).replace(',', '.')); } catch (Throwable e) { return 0d; } }
    private static String one(double v) { return String.format(Locale.getDefault(), "%.2f", v); }
    private static String shortMsg(Throwable e) { String m = e == null ? "erro" : e.getMessage(); return m == null || m.trim().isEmpty() ? e.getClass().getSimpleName() : (m.length() > 90 ? m.substring(0, 90) : m); }
    private static String clean(Throwable e) { return shortMsg(e).replaceAll("(?i)password=[^&\\s]+", "password=***"); }
    private static JSONObject fail(String m) { JSONObject o = new JSONObject(); try { o.put("ok", false); o.put("error", m); } catch (Throwable ignored) {} return o; }
    private static void send(ResultReceiver receiver, JSONObject payload) { if (receiver == null) return; try { Bundle b = new Bundle(); b.putString("json", payload == null ? "{}" : payload.toString()); receiver.send(CODE_RESULT, b); } catch (Throwable ignored) {} }
}
