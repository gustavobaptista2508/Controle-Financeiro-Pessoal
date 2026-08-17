package com.granaok.app;

import android.annotation.SuppressLint;
import android.content.ClipData;
import android.content.ContentResolver;
import android.content.Intent;
import android.content.SharedPreferences;
import android.database.Cursor;
import android.graphics.Bitmap;
import android.graphics.Color;
import android.graphics.Matrix;
import android.graphics.pdf.PdfRenderer;
import android.net.Uri;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.os.ParcelFileDescriptor;
import android.os.ResultReceiver;
import android.provider.MediaStore;
import android.provider.OpenableColumns;
import android.util.Base64;
import android.view.View;
import android.view.ViewGroup;
import android.webkit.JavascriptInterface;
import android.webkit.WebView;

import androidx.core.content.FileProvider;

import com.google.android.gms.tasks.Tasks;
import com.google.mlkit.vision.common.InputImage;
import com.google.mlkit.vision.text.Text;
import com.google.mlkit.vision.text.TextRecognition;
import com.google.mlkit.vision.text.TextRecognizer;
import com.google.mlkit.vision.text.latin.TextRecognizerOptions;

import org.json.JSONObject;

import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.InputStream;
import java.nio.charset.Charset;
import java.nio.charset.StandardCharsets;
import java.security.KeyStore;
import java.util.Locale;
import java.util.concurrent.TimeUnit;

import javax.crypto.Cipher;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

/**
 * Beta 0.4.0: OCR de comprovantes, importação de extratos e Radar de Investimentos.
 * Mantém todas as bridges das versões anteriores por herança.
 */
public class MainActivityV040 extends MainActivityV039 {
    private static final String PREFS = "granaok_secure";
    private static final String KEY_ALIAS = "granaok_db_key";
    private static final int REQ_PICK_RECEIPT = 4101;
    private static final int REQ_CAPTURE_RECEIPT = 4102;
    private static final int REQ_PICK_STATEMENT = 4103;
    private WebView web040;
    private Uri pendingCameraUri;

    @SuppressLint({"SetJavaScriptEnabled", "JavascriptInterface"})
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        web040 = findWebViewRecursive(findViewById(android.R.id.content));
        if (web040 != null) {
            web040.addJavascriptInterface(new ImportBridge(), "GranaImport");
            web040.addJavascriptInterface(new InvestBridge(), "GranaInvest");
        }
    }

    @Override
    protected void onDestroy() {
        if (web040 != null) {
            try { web040.removeJavascriptInterface("GranaImport"); } catch (Throwable ignored) {}
            try { web040.removeJavascriptInterface("GranaInvest"); } catch (Throwable ignored) {}
        }
        super.onDestroy();
    }

    private WebView findWebViewRecursive(View view) {
        if (view instanceof WebView) return (WebView) view;
        if (view instanceof ViewGroup) {
            ViewGroup g = (ViewGroup) view;
            for (int i = 0; i < g.getChildCount(); i++) {
                WebView w = findWebViewRecursive(g.getChildAt(i)); if (w != null) return w;
            }
        }
        return null;
    }

    public class ImportBridge {
        @JavascriptInterface public boolean isAvailable() { return true; }

        @JavascriptInterface public void chooseReceiptImage() {
            runOnUiThread(() -> {
                try {
                    Intent i = new Intent(Intent.ACTION_OPEN_DOCUMENT); i.addCategory(Intent.CATEGORY_OPENABLE); i.setType("image/*");
                    startActivityForResult(i, REQ_PICK_RECEIPT);
                } catch (Throwable e) { callback("GranaOkReceiptScan", failure("Não foi possível abrir a galeria: " + clean(e))); }
            });
        }

        @JavascriptInterface public void takeReceiptPhoto() {
            runOnUiThread(() -> {
                try {
                    File dir = new File(getCacheDir(), "receipt_capture"); if (!dir.exists()) dir.mkdirs();
                    File file = new File(dir, "receipt_" + System.currentTimeMillis() + ".jpg");
                    pendingCameraUri = FileProvider.getUriForFile(MainActivityV040.this, getPackageName() + ".files", file);
                    Intent i = new Intent(MediaStore.ACTION_IMAGE_CAPTURE); i.putExtra(MediaStore.EXTRA_OUTPUT, pendingCameraUri);
                    i.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION | Intent.FLAG_GRANT_WRITE_URI_PERMISSION);
                    i.setClipData(ClipData.newRawUri("receipt", pendingCameraUri));
                    startActivityForResult(i, REQ_CAPTURE_RECEIPT);
                } catch (Throwable e) { callback("GranaOkReceiptScan", failure("Não foi possível abrir a câmera: " + clean(e))); }
            });
        }

        @JavascriptInterface public void chooseBankStatement() {
            runOnUiThread(() -> {
                try {
                    Intent i = new Intent(Intent.ACTION_OPEN_DOCUMENT); i.addCategory(Intent.CATEGORY_OPENABLE); i.setType("*/*");
                    i.putExtra(Intent.EXTRA_MIME_TYPES, new String[]{"application/pdf","text/plain","text/csv","application/csv","application/x-ofx","application/vnd.intu.qfx","application/octet-stream","image/*"});
                    startActivityForResult(i, REQ_PICK_STATEMENT);
                } catch (Throwable e) { callback("GranaOkStatementParsed", failure("Não foi possível escolher o extrato: " + clean(e))); }
            });
        }

        @JavascriptInterface public void loadAccounts() {
            requestFinanceExtras(FinanceExtrasService.ACTION_LIST_ACCOUNTS, null, "GranaOkImportAccounts");
        }

        @JavascriptInterface public void importStatement(String payloadJson) {
            requestImport(payloadJson);
        }
    }

    public class InvestBridge {
        @JavascriptInterface public boolean isAvailable() { return true; }
        @JavascriptInterface public void refresh() { requestRadar(); }
        @JavascriptInterface public void openOfficialSource(String url) { runOnUiThread(() -> openWhitelisted(url)); }
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (resultCode != RESULT_OK) {
            if (requestCode == REQ_PICK_RECEIPT || requestCode == REQ_CAPTURE_RECEIPT) callback("GranaOkImportCancelled", "{}");
            return;
        }
        try {
            if (requestCode == REQ_PICK_RECEIPT) {
                Uri uri = data == null ? null : data.getData(); if (uri != null) takeReadPermission(data, uri); scanReceipt(uri);
            } else if (requestCode == REQ_CAPTURE_RECEIPT) {
                scanReceipt(pendingCameraUri);
            } else if (requestCode == REQ_PICK_STATEMENT) {
                Uri uri = data == null ? null : data.getData(); if (uri != null) takeReadPermission(data, uri); parseStatementUri(uri);
            }
        } catch (Throwable e) {
            String payload = failure(clean(e));
            if (requestCode == REQ_PICK_STATEMENT) callback("GranaOkStatementParsed", payload); else callback("GranaOkReceiptScan", payload);
        }
    }

    private void takeReadPermission(Intent data, Uri uri) {
        if (data == null || uri == null) return;
        try { getContentResolver().takePersistableUriPermission(uri, data.getFlags() & Intent.FLAG_GRANT_READ_URI_PERMISSION); } catch (Throwable ignored) {}
    }

    private void scanReceipt(Uri uri) {
        if (uri == null) { callback("GranaOkReceiptScan", failure("Imagem não recebida.")); return; }
        callback("GranaOkImportProgress", progress("ocr", "Lendo o comprovante no aparelho..."));
        try {
            InputImage image = InputImage.fromFilePath(this, uri);
            TextRecognizer recognizer = TextRecognition.getClient(TextRecognizerOptions.DEFAULT_OPTIONS);
            recognizer.process(image)
                .addOnSuccessListener(result -> {
                    try { callback("GranaOkReceiptScan", DocumentParser.parseReceipt(result.getText()).toString()); }
                    catch (Throwable e) { callback("GranaOkReceiptScan", failure(clean(e))); }
                    finally { try { recognizer.close(); } catch (Throwable ignored) {} }
                })
                .addOnFailureListener(e -> { callback("GranaOkReceiptScan", failure("OCR não conseguiu ler a imagem: " + clean(e))); try { recognizer.close(); } catch (Throwable ignored) {} });
        } catch (Throwable e) { callback("GranaOkReceiptScan", failure("Não foi possível abrir a imagem: " + clean(e))); }
    }

    private void parseStatementUri(Uri uri) {
        if (uri == null) { callback("GranaOkStatementParsed", failure("Arquivo não recebido.")); return; }
        callback("GranaOkImportProgress", progress("statement", "Lendo o extrato..."));
        new Thread(() -> {
            try {
                String name = displayName(uri); String type = getContentResolver().getType(uri); String lower = name.toLowerCase(Locale.ROOT);
                String text;
                if ((type != null && type.equals("application/pdf")) || lower.endsWith(".pdf")) text = ocrPdf(uri);
                else if ((type != null && type.startsWith("image/")) || lower.matches(".*\\.(jpg|jpeg|png|webp)$")) text = ocrImageBlocking(uri);
                else text = readText(uri);
                JSONObject parsed = DocumentParser.parseStatement(text, name); parsed.put("mime", type == null ? "" : type);
                callback("GranaOkStatementParsed", parsed.toString());
            } catch (Throwable e) { callback("GranaOkStatementParsed", failure("Não foi possível interpretar o extrato: " + clean(e))); }
        }, "GranaOk-Statement-Reader").start();
    }

    private String ocrImageBlocking(Uri uri) throws Exception {
        TextRecognizer r = TextRecognition.getClient(TextRecognizerOptions.DEFAULT_OPTIONS);
        try { Text text = Tasks.await(r.process(InputImage.fromFilePath(this, uri)), 25, TimeUnit.SECONDS); return text.getText(); }
        finally { try { r.close(); } catch (Throwable ignored) {} }
    }

    private String ocrPdf(Uri uri) throws Exception {
        ParcelFileDescriptor fd = getContentResolver().openFileDescriptor(uri, "r"); if (fd == null) throw new IllegalStateException("PDF não pôde ser aberto.");
        PdfRenderer renderer = new PdfRenderer(fd); TextRecognizer recognizer = TextRecognition.getClient(TextRecognizerOptions.DEFAULT_OPTIONS);
        StringBuilder all = new StringBuilder(); int pages = Math.min(renderer.getPageCount(), 10);
        try {
            for (int i = 0; i < pages; i++) {
                final int pageNo = i + 1; callback("GranaOkImportProgress", progress("pdf", "Lendo página " + pageNo + " de " + pages + "..."));
                PdfRenderer.Page page = renderer.openPage(i);
                float scale = Math.min(2.2f, 1600f / Math.max(1, page.getWidth()));
                int w = Math.max(700, Math.round(page.getWidth() * scale)); int h = Math.max(900, Math.round(page.getHeight() * scale));
                Bitmap bitmap = Bitmap.createBitmap(w, h, Bitmap.Config.ARGB_8888); bitmap.eraseColor(Color.WHITE);
                Matrix matrix = new Matrix(); matrix.setScale((float) w / page.getWidth(), (float) h / page.getHeight());
                page.render(bitmap, null, matrix, PdfRenderer.Page.RENDER_MODE_FOR_DISPLAY); page.close();
                Text t = Tasks.await(recognizer.process(InputImage.fromBitmap(bitmap, 0)), 25, TimeUnit.SECONDS); all.append(t.getText()).append('\n'); bitmap.recycle();
            }
            if (renderer.getPageCount() > pages) all.append("\n[GranaOk: somente as primeiras ").append(pages).append(" páginas foram analisadas]\n");
            return all.toString();
        } finally { try { recognizer.close(); } catch (Throwable ignored) {} try { renderer.close(); } catch (Throwable ignored) {} try { fd.close(); } catch (Throwable ignored) {} }
    }

    private String readText(Uri uri) throws Exception {
        InputStream in = getContentResolver().openInputStream(uri); if (in == null) throw new IllegalStateException("Arquivo não pôde ser aberto.");
        ByteArrayOutputStream out = new ByteArrayOutputStream(); byte[] buffer = new byte[8192]; int n; int max = 6 * 1024 * 1024;
        while ((n = in.read(buffer)) > 0 && out.size() < max) out.write(buffer, 0, Math.min(n, max - out.size())); in.close();
        byte[] bytes = out.toByteArray(); String utf = new String(bytes, StandardCharsets.UTF_8);
        int replacements = 0; for (int i = 0; i < utf.length(); i++) if (utf.charAt(i) == '\uFFFD') replacements++;
        if (replacements > Math.max(2, utf.length() / 500)) return new String(bytes, Charset.forName("Windows-1252"));
        return utf;
    }

    private String displayName(Uri uri) {
        String name = "extrato"; Cursor c = null;
        try { c = getContentResolver().query(uri, new String[]{OpenableColumns.DISPLAY_NAME}, null, null, null); if (c != null && c.moveToFirst()) { String n = c.getString(0); if (n != null && !n.trim().isEmpty()) name = n; } }
        catch (Throwable ignored) {} finally { if (c != null) c.close(); }
        return name;
    }

    private void requestFinanceExtras(String action, String payloadJson, String callbackName) {
        try {
            ResultReceiver receiver = receiverFor(callbackName); Intent intent = new Intent(this, FinanceExtrasService.class);
            intent.putExtra(FinanceExtrasService.EXTRA_ACTION, action); intent.putExtra(FinanceExtrasService.EXTRA_CONFIG, readSecureConfig().toString());
            if (payloadJson != null) intent.putExtra(FinanceExtrasService.EXTRA_PAYLOAD, payloadJson); intent.putExtra(FinanceExtrasService.EXTRA_RECEIVER, receiver); startService(intent);
        } catch (Throwable e) { callback(callbackName, failure(clean(e))); }
    }

    private void requestImport(String payloadJson) {
        try {
            Intent intent = new Intent(this, ImportFinanceService.class); intent.putExtra(ImportFinanceService.EXTRA_CONFIG, readSecureConfig().toString());
            intent.putExtra(ImportFinanceService.EXTRA_PAYLOAD, payloadJson); intent.putExtra(ImportFinanceService.EXTRA_RECEIVER, receiverFor("GranaOkStatementImported")); startService(intent);
        } catch (Throwable e) { callback("GranaOkStatementImported", failure(clean(e))); }
    }

    private void requestRadar() {
        try { Intent i = new Intent(this, InvestmentRadarService.class); i.putExtra(InvestmentRadarService.EXTRA_RECEIVER, receiverFor("GranaOkInvestmentRadar")); startService(i); }
        catch (Throwable e) { callback("GranaOkInvestmentRadar", failure(clean(e))); }
    }

    private ResultReceiver receiverFor(String fn) {
        return new ResultReceiver(new Handler(Looper.getMainLooper())) {
            @Override protected void onReceiveResult(int resultCode, Bundle resultData) { callback(fn, resultData == null ? "{}" : resultData.getString("json", "{}")); }
        };
    }

    private JSONObject readSecureConfig() throws Exception {
        SharedPreferences prefs = getSharedPreferences(PREFS, MODE_PRIVATE); String ivB64 = prefs.getString("db_iv", null); String cipherB64 = prefs.getString("db_cipher", null);
        if (ivB64 == null || cipherB64 == null) throw new IllegalStateException("Banco de dados ainda não configurado.");
        KeyStore ks = KeyStore.getInstance("AndroidKeyStore"); ks.load(null); KeyStore.SecretKeyEntry entry = (KeyStore.SecretKeyEntry) ks.getEntry(KEY_ALIAS, null);
        if (entry == null) throw new IllegalStateException("Chave segura do banco não encontrada."); SecretKey key = entry.getSecretKey();
        Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding"); cipher.init(Cipher.DECRYPT_MODE, key, new GCMParameterSpec(128, Base64.decode(ivB64, Base64.NO_WRAP)));
        return new JSONObject(new String(cipher.doFinal(Base64.decode(cipherB64, Base64.NO_WRAP)), StandardCharsets.UTF_8));
    }

    private void callback(String fn, String json) {
        if (web040 == null || fn == null || fn.trim().isEmpty()) return; final String js = "window." + fn + " && window." + fn + "(" + JSONObject.quote(json == null ? "{}" : json) + ")";
        runOnUiThread(() -> { if (web040 != null) try { web040.evaluateJavascript(js, null); } catch (Throwable ignored) {} });
    }

    private void openWhitelisted(String raw) {
        try {
            Uri uri = Uri.parse(raw); String host = uri.getHost(); if (host == null) return; host = host.toLowerCase(Locale.ROOT);
            if (!(host.endsWith("bcb.gov.br") || host.endsWith("b3.com.br") || host.endsWith("tesourodireto.com.br"))) return;
            startActivity(new Intent(Intent.ACTION_VIEW, uri));
        } catch (Throwable ignored) {}
    }

    private static String progress(String stage, String message) { JSONObject o = new JSONObject(); try { o.put("stage", stage); o.put("message", message); } catch (Throwable ignored) {} return o.toString(); }
    private static String failure(String message) { JSONObject o = new JSONObject(); try { o.put("ok", false); o.put("error", message); } catch (Throwable ignored) {} return o.toString(); }
    private static String clean(Throwable e) { String m = e == null ? "Falha desconhecida" : e.getMessage(); if (m == null || m.trim().isEmpty()) m = e == null ? "Falha desconhecida" : e.getClass().getSimpleName(); m = m.replaceAll("(?i)password=[^&\\s]+", "password=***"); return m.length() > 420 ? m.substring(0, 420) : m; }
}
