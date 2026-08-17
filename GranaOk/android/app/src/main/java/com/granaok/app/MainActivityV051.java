package com.granaok.app;

import android.annotation.SuppressLint;
import android.content.Intent;
import android.database.Cursor;
import android.net.Uri;
import android.os.Bundle;
import android.provider.OpenableColumns;
import android.view.View;
import android.view.ViewGroup;
import android.webkit.WebView;

import com.tom_roush.pdfbox.android.PDFBoxResourceLoader;
import com.tom_roush.pdfbox.pdmodel.PDDocument;
import com.tom_roush.pdfbox.text.PDFTextStripper;

import org.json.JSONObject;

import java.io.InputStream;
import java.util.Locale;
import java.util.regex.Pattern;

/**
 * Beta 0.5.1: PDF bancário usa primeiro a camada de texto do arquivo.
 * OCR da 0.4/0.5 permanece como fallback para PDFs escaneados.
 */
public class MainActivityV051 extends MainActivityV050 {
    private static final int REQ_PICK_STATEMENT = 4103;
    private static final Pattern HAS_DATE = Pattern.compile("(?s).*\\b\\d{2}[/-]\\d{2}(?:[/-]\\d{2,4})?\\b.*");
    private static final Pattern HAS_MONEY = Pattern.compile("(?s).*(?:\\d{1,3}(?:\\.\\d{3})+|\\d+),\\d{2}.*");
    private WebView web051;

    @SuppressLint("SetJavaScriptEnabled")
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        web051 = findWebViewRecursive(findViewById(android.R.id.content));
        try { PDFBoxResourceLoader.init(getApplicationContext()); } catch (Throwable ignored) {}
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        if (requestCode != REQ_PICK_STATEMENT || resultCode != RESULT_OK || data == null || data.getData() == null) {
            super.onActivityResult(requestCode, resultCode, data);
            return;
        }

        final Uri uri = data.getData();
        final String name = displayName(uri);
        final String mime = getContentResolver().getType(uri);
        final String lower = name.toLowerCase(Locale.ROOT);
        final boolean isPdf = (mime != null && "application/pdf".equalsIgnoreCase(mime)) || lower.endsWith(".pdf");

        if (!isPdf) {
            super.onActivityResult(requestCode, resultCode, data);
            return;
        }

        try {
            int flags = data.getFlags() & Intent.FLAG_GRANT_READ_URI_PERMISSION;
            getContentResolver().takePersistableUriPermission(uri, flags);
        } catch (Throwable ignored) {}

        callback051("GranaOkImportProgress", progress("pdf_text", "Lendo o texto original do PDF..."));
        new Thread(() -> {
            try {
                String text = extractPdfText(uri);
                if (!looksUseful(text)) throw new IllegalStateException("PDF sem camada de texto suficiente");

                JSONObject parsed;
                if (SicoobStatementParser.isSupported(text)) parsed = SicoobStatementParser.parse(text, name);
                else parsed = DocumentParser.parseStatement(text, name);

                parsed.put("mime", mime == null ? "application/pdf" : mime);
                parsed.put("reader", "pdf_text");
                if (parsed.optInt("count", 0) <= 0) throw new IllegalStateException("Nenhum lançamento encontrado na camada de texto");
                callback051("GranaOkStatementParsed", parsed.toString());
            } catch (Throwable textError) {
                callback051("GranaOkImportProgress", progress("ocr_fallback", "Texto do PDF não foi suficiente. Tentando OCR..."));
                fallbackToLegacy(requestCode, resultCode, data);
            }
        }, "GranaOk-PDF-Text").start();
    }

    private String extractPdfText(Uri uri) throws Exception {
        PDFBoxResourceLoader.init(getApplicationContext());
        InputStream in = getContentResolver().openInputStream(uri);
        if (in == null) throw new IllegalStateException("PDF não pôde ser aberto.");
        PDDocument document = null;
        try {
            document = PDDocument.load(in);
            PDFTextStripper stripper = new PDFTextStripper();
            stripper.setSortByPosition(true);
            stripper.setStartPage(1);
            stripper.setEndPage(Math.min(document.getNumberOfPages(), 30));
            return stripper.getText(document);
        } finally {
            try { if (document != null) document.close(); } catch (Throwable ignored) {}
            try { in.close(); } catch (Throwable ignored) {}
        }
    }

    private boolean looksUseful(String text) {
        if (text == null || text.trim().length() < 100) return false;
        return HAS_DATE.matcher(text).matches() && HAS_MONEY.matcher(text).matches();
    }

    private void fallbackToLegacy(int requestCode, int resultCode, Intent data) {
        runOnUiThread(() -> legacyActivityResult(requestCode, resultCode, data));
    }

    private void legacyActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
    }

    private String displayName(Uri uri) {
        String name = "extrato.pdf";
        Cursor cursor = null;
        try {
            cursor = getContentResolver().query(uri, new String[]{OpenableColumns.DISPLAY_NAME}, null, null, null);
            if (cursor != null && cursor.moveToFirst()) {
                String found = cursor.getString(0);
                if (found != null && !found.trim().isEmpty()) name = found.trim();
            }
        } catch (Throwable ignored) {
        } finally {
            if (cursor != null) try { cursor.close(); } catch (Throwable ignored) {}
        }
        return name;
    }

    private WebView findWebViewRecursive(View view) {
        if (view instanceof WebView) return (WebView) view;
        if (view instanceof ViewGroup) {
            ViewGroup group = (ViewGroup) view;
            for (int i = 0; i < group.getChildCount(); i++) {
                WebView web = findWebViewRecursive(group.getChildAt(i));
                if (web != null) return web;
            }
        }
        return null;
    }

    private void callback051(String fn, String json) {
        if (web051 == null || fn == null || fn.trim().isEmpty()) return;
        final String js = "window." + fn + " && window." + fn + "(" + JSONObject.quote(json == null ? "{}" : json) + ")";
        runOnUiThread(() -> {
            if (web051 != null) try { web051.evaluateJavascript(js, null); } catch (Throwable ignored) {}
        });
    }

    private static String progress(String stage, String message) {
        JSONObject out = new JSONObject();
        try { out.put("stage", stage); out.put("message", message); } catch (Throwable ignored) {}
        return out.toString();
    }
}
