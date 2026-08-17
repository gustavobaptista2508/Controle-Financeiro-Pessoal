package com.granaok.app;

import org.json.JSONArray;
import org.json.JSONObject;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.text.Normalizer;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Calendar;
import java.util.Date;
import java.util.HashSet;
import java.util.List;
import java.util.Locale;
import java.util.Set;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/** Parsers locais usados pela importação do GranaOk. */
public final class DocumentParser {
    private DocumentParser() {}

    private static final Pattern DATE_DMY = Pattern.compile("\\b(\\d{2})[/-](\\d{2})[/-](\\d{2,4})\\b");
    private static final Pattern DATE_DM = Pattern.compile("\\b(\\d{2})[/-](\\d{2})\\b");
    private static final Pattern DATE_YMD = Pattern.compile("\\b(20\\d{2})-(\\d{2})-(\\d{2})\\b");
    private static final Pattern MONEY_BR = Pattern.compile("(?<!\\d)([-+]?\\s*(?:R\\$\\s*)?(?:\\d{1,3}(?:\\.\\d{3})+|\\d+),\\d{2})(?!\\d)", Pattern.CASE_INSENSITIVE);

    public static JSONObject parseReceipt(String raw) throws Exception {
        String text = cleanRaw(raw);
        String[] lines = text.split("\\r?\\n");
        JSONObject out = new JSONObject();
        out.put("ok", true);
        out.put("kind", "receipt");
        out.put("merchant", guessMerchant(lines));
        out.put("date", guessDate(text));
        BigDecimal total = guessReceiptTotal(lines);
        if (total != null) out.put("total", total.doubleValue());
        out.put("payment", guessPayment(text));
        out.put("card_last4", findGroup(text, Pattern.compile("(?i)(?:final|cart[aã]o|card|\\*{2,}|x{2,})[^\\n\\d]{0,12}(\\d{4})\\b"), 1));
        out.put("nsu", findGroup(text, Pattern.compile("(?i)\\bNSU\\s*[:#-]?\\s*([A-Z0-9-]{3,24})"), 1));
        out.put("authorization", findGroup(text, Pattern.compile("(?i)(?:AUTORIZA(?:C|Ç)[AÃ]O|AUT|COD\\.?\\s*AUT)\\s*[:#-]?\\s*([A-Z0-9-]{3,24})"), 1));
        out.put("raw_text", truncate(text, 16000));
        return out;
    }

    public static JSONObject parseStatement(String raw, String fileName) throws Exception {
        String text = cleanRaw(raw);
        String normalized = fold(text);
        if (normalized.contains("<stmttrn>") || normalized.contains("<ofx>")) return parseOfx(text, fileName);
        String first = firstNonEmptyLine(text);
        if (looksLikeCsvHeader(first)) {
            JSONObject csv = parseCsv(text, fileName);
            JSONArray rows = csv.optJSONArray("rows");
            if (rows != null && rows.length() > 0) return csv;
        }
        return parseGenericStatement(text, fileName);
    }

    private static JSONObject parseOfx(String text, String fileName) throws Exception {
        JSONArray rows = new JSONArray();
        Set<String> seen = new HashSet<String>();
        Matcher blocks = Pattern.compile("(?is)<STMTTRN>(.*?)(?=<STMTTRN>|</BANKTRANLIST>|</OFX>|$)").matcher(text);
        while (blocks.find() && rows.length() < 800) {
            String block = blocks.group(1);
            BigDecimal signed = parseFlexibleMoney(ofxTag(block, "TRNAMT"));
            if (signed == null || signed.compareTo(BigDecimal.ZERO) == 0) continue;
            String date = normalizeOfxDate(ofxTag(block, "DTPOSTED"));
            if (date.isEmpty()) continue;
            String name = firstNonEmpty(ofxTag(block, "NAME"), ofxTag(block, "MEMO"), ofxTag(block, "CHECKNUM"), "Lançamento bancário");
            String memo = ofxTag(block, "MEMO");
            String fitid = ofxTag(block, "FITID");
            String type = signed.signum() < 0 ? "expense" : "income";
            BigDecimal amount = signed.abs().setScale(2, RoundingMode.HALF_UP);
            if (!seen.add(date + "|" + fold(name) + "|" + type + "|" + amount.toPlainString())) continue;
            JSONObject row = new JSONObject();
            row.put("date", date); row.put("description", tidyDescription(name)); row.put("type", type); row.put("amount", amount.doubleValue());
            if (!fitid.isEmpty()) row.put("external_id", fitid);
            if (!memo.isEmpty() && !memo.equalsIgnoreCase(name)) row.put("observations", truncate(memo, 500));
            rows.put(row);
        }
        return statementResult(rows, fileName, "OFX/QFX");
    }

    private static JSONObject parseCsv(String text, String fileName) throws Exception {
        String[] lines = text.split("\\r?\\n");
        int headerIndex = -1; char delimiter = ';'; List<String> header = null;
        for (int i = 0; i < Math.min(lines.length, 20); i++) {
            String line = lines[i].trim(); if (line.isEmpty()) continue;
            char d = detectDelimiter(line); List<String> cells = splitCsv(line, d); String joined = fold(join(cells, " "));
            if ((joined.contains("data") || joined.contains("date")) &&
                (joined.contains("valor") || joined.contains("amount") || joined.contains("debito") || joined.contains("credito")) &&
                (joined.contains("descricao") || joined.contains("historico") || joined.contains("lancamento") || joined.contains("memo") || joined.contains("nome"))) {
                headerIndex = i; delimiter = d; header = cells; break;
            }
        }
        if (headerIndex < 0 || header == null) return statementResult(new JSONArray(), fileName, "CSV");

        int dateCol = findColumn(header, "data", "date", "dt");
        int descCol = findColumn(header, "descricao", "historico", "lancamento", "memo", "nome", "description");
        int valueCol = findColumn(header, "valor", "amount", "value");
        int debitCol = findColumn(header, "debito", "debitos", "debit", "saida");
        int creditCol = findColumn(header, "credito", "creditos", "credit", "entrada");
        int idCol = findColumn(header, "fitid", "id", "identificador", "documento");
        JSONArray rows = new JSONArray(); Set<String> seen = new HashSet<String>();

        for (int i = headerIndex + 1; i < lines.length && rows.length() < 800; i++) {
            if (lines[i].trim().isEmpty()) continue;
            List<String> cells = splitCsv(lines[i], delimiter);
            String date = normalizeDate(cell(cells, dateCol)); String desc = tidyDescription(cell(cells, descCol));
            if (date.isEmpty() || desc.isEmpty() || isBalanceLine(desc)) continue;
            BigDecimal amount = null; String type = null;
            if (debitCol >= 0 || creditCol >= 0) {
                BigDecimal debit = parseFlexibleMoney(cell(cells, debitCol)); BigDecimal credit = parseFlexibleMoney(cell(cells, creditCol));
                if (debit != null && debit.abs().compareTo(BigDecimal.ZERO) > 0) { amount = debit.abs(); type = "expense"; }
                else if (credit != null && credit.abs().compareTo(BigDecimal.ZERO) > 0) { amount = credit.abs(); type = "income"; }
            }
            if (amount == null && valueCol >= 0) {
                BigDecimal v = parseFlexibleMoney(cell(cells, valueCol));
                if (v != null && v.compareTo(BigDecimal.ZERO) != 0) { type = v.signum() < 0 ? "expense" : "income"; amount = v.abs(); }
            }
            if (amount == null || type == null) continue;
            amount = amount.setScale(2, RoundingMode.HALF_UP);
            if (!seen.add(date + "|" + fold(desc) + "|" + type + "|" + amount.toPlainString())) continue;
            JSONObject row = new JSONObject(); row.put("date", date); row.put("description", desc); row.put("type", type); row.put("amount", amount.doubleValue());
            String external = cell(cells, idCol).trim(); if (!external.isEmpty()) row.put("external_id", external); rows.put(row);
        }
        return statementResult(rows, fileName, "CSV");
    }

    private static JSONObject parseGenericStatement(String text, String fileName) throws Exception {
        JSONArray rows = new JSONArray(); Set<String> seen = new HashSet<String>(); String[] lines = text.split("\\r?\\n");
        int inferredYear = Calendar.getInstance().get(Calendar.YEAR);
        Pattern br = Pattern.compile("^\\s*(\\d{2}[/-]\\d{2}(?:[/-]\\d{2,4})?)\\s+(.+?)\\s+([-+]?\\s*(?:R\\$\\s*)?(?:\\d{1,3}(?:\\.\\d{3})+|\\d+),\\d{2})(?:\\s*([DC]))?\\s*$", Pattern.CASE_INSENSITIVE);
        Pattern dot = Pattern.compile("^\\s*(\\d{2}[/-]\\d{2}(?:[/-]\\d{2,4})?)\\s+(.+?)\\s+([-+]?\\s*(?:R\\$\\s*)?\\d+\\.\\d{2})(?:\\s*([DC]))?\\s*$", Pattern.CASE_INSENSITIVE);
        for (String line : lines) {
            if (rows.length() >= 800) break; String s = line.trim(); if (s.length() < 8) continue;
            Matcher m = br.matcher(s); if (!m.matches()) { m = dot.matcher(s); if (!m.matches()) continue; }
            String date = normalizeDateWithYear(m.group(1), inferredYear); String desc = tidyDescription(m.group(2));
            if (date.isEmpty() || desc.isEmpty() || isBalanceLine(desc)) continue;
            BigDecimal signed = parseFlexibleMoney(m.group(3)); if (signed == null || signed.compareTo(BigDecimal.ZERO) == 0) continue;
            String dc = m.group(4); String type = dc != null ? (dc.equalsIgnoreCase("D") ? "expense" : "income") : (signed.signum() < 0 ? "expense" : "income");
            BigDecimal amount = signed.abs().setScale(2, RoundingMode.HALF_UP);
            if (!seen.add(date + "|" + fold(desc) + "|" + type + "|" + amount.toPlainString())) continue;
            JSONObject row = new JSONObject(); row.put("date", date); row.put("description", desc); row.put("type", type); row.put("amount", amount.doubleValue()); rows.put(row);
        }
        return statementResult(rows, fileName, "PDF/TXT por leitura de texto");
    }

    private static JSONObject statementResult(JSONArray rows, String fileName, String format) throws Exception {
        JSONObject out = new JSONObject(); out.put("ok", true); out.put("kind", "statement"); out.put("format", format);
        out.put("file_name", fileName == null ? "extrato" : fileName); out.put("rows", rows); out.put("count", rows.length()); return out;
    }

    private static BigDecimal guessReceiptTotal(String[] lines) {
        BigDecimal best = null; int bestScore = Integer.MIN_VALUE;
        for (int i = 0; i < lines.length; i++) {
            String line = lines[i], f = fold(line); Matcher m = MONEY_BR.matcher(line);
            while (m.find()) {
                BigDecimal v = parseFlexibleMoney(m.group(1)); if (v == null || v.signum() <= 0) continue;
                int score = i;
                if (f.contains("valor total") || f.matches(".*\\btotal\\b.*")) score += 200;
                if (f.contains("valor pago") || f.contains("a pagar") || f.contains("valor da compra")) score += 170;
                if (f.contains("troco") || f.contains("desconto") || f.contains("subtotal") || f.contains("taxa")) score -= 80;
                if (score > bestScore) { bestScore = score; best = v; }
            }
        }
        return best == null ? null : best.setScale(2, RoundingMode.HALF_UP);
    }

    private static String guessMerchant(String[] lines) {
        for (int i = 0; i < Math.min(lines.length, 12); i++) {
            String s = lines[i].trim(), f = fold(s); if (s.length() < 3 || s.length() > 80) continue;
            if (f.matches(".*(cnpj|cpf|cupom|fiscal|sat|nfce|nfc-e|via cliente|comprovante|data|hora|terminal|estabelecimento).*")) continue;
            if (MONEY_BR.matcher(s).find() || DATE_DMY.matcher(s).find() || s.matches(".*\\d{8,}.*")) continue;
            return tidyDescription(s);
        }
        return "Compra / comprovante";
    }

    private static String guessDate(String text) {
        Matcher ymd = DATE_YMD.matcher(text); if (ymd.find()) return ymd.group(1) + "-" + ymd.group(2) + "-" + ymd.group(3);
        Matcher dmy = DATE_DMY.matcher(text); if (dmy.find()) return toIso(dmy.group(1), dmy.group(2), dmy.group(3));
        return new SimpleDateFormat("yyyy-MM-dd", Locale.US).format(new Date());
    }

    private static String guessPayment(String text) {
        String f = fold(text); if (f.contains("pix")) return "PIX"; if (f.contains("credito") || f.contains("credit")) return "Cartão de crédito";
        if (f.contains("debito") || f.contains("debit")) return "Cartão de débito"; if (f.contains("dinheiro")) return "Dinheiro"; if (f.contains("voucher")) return "Voucher"; return "Não identificado";
    }

    private static String ofxTag(String block, String tag) { Matcher m = Pattern.compile("(?is)<" + Pattern.quote(tag) + ">\\s*([^<\\r\\n]+)").matcher(block); return m.find() ? m.group(1).trim() : ""; }
    private static String normalizeOfxDate(String value) { if (value == null) return ""; Matcher m = Pattern.compile("(\\d{4})(\\d{2})(\\d{2})").matcher(value); return m.find() ? m.group(1) + "-" + m.group(2) + "-" + m.group(3) : ""; }
    private static boolean looksLikeCsvHeader(String line) { String f = fold(line); return (line.contains(";") || line.contains(",") || line.contains("\t")) && (f.contains("data") || f.contains("date")) && (f.contains("valor") || f.contains("amount") || f.contains("debito") || f.contains("credito")); }
    private static char detectDelimiter(String line) { int semi=count(line,';'), comma=count(line,','), tab=count(line,'\t'); if(tab>semi&&tab>comma)return '\t'; return semi>=comma?';':','; }

    private static List<String> splitCsv(String line, char delimiter) {
        List<String> out=new ArrayList<String>(); StringBuilder cur=new StringBuilder(); boolean quoted=false;
        for(int i=0;i<line.length();i++){char ch=line.charAt(i);if(ch=='"'){if(quoted&&i+1<line.length()&&line.charAt(i+1)=='"'){cur.append('"');i++;}else quoted=!quoted;}else if(ch==delimiter&&!quoted){out.add(cur.toString().trim());cur.setLength(0);}else cur.append(ch);} out.add(cur.toString().trim());return out;
    }

    private static int findColumn(List<String> header,String... names){for(int i=0;i<header.size();i++){String h=fold(header.get(i));for(String n:names)if(h.equals(n)||h.contains(n))return i;}return -1;}
    private static String cell(List<String> cells,int index){return index>=0&&index<cells.size()?cells.get(index):"";}

    public static BigDecimal parseFlexibleMoney(String raw) {
        if(raw==null)return null;String s=raw.trim().replace("R$","").replace(" ","");if(s.isEmpty())return null;
        boolean paren=s.startsWith("(")&&s.endsWith(")");s=s.replace("(","").replace(")","");
        try{if(s.contains(","))s=s.replace(".","").replace(',','.');else{int dots=count(s,'.');if(dots>1)s=s.replace(".","");else if(dots==1&&s.matches("[-+]?\\d{1,3}\\.\\d{3}"))s=s.replace(".","");}BigDecimal v=new BigDecimal(s);return paren?v.negate():v;}catch(Throwable ignored){return null;}
    }

    private static String normalizeDate(String raw){return normalizeDateWithYear(raw,Calendar.getInstance().get(Calendar.YEAR));}
    private static String normalizeDateWithYear(String raw,int inferredYear){if(raw==null)return "";String s=raw.trim();Matcher ymd=DATE_YMD.matcher(s);if(ymd.find())return ymd.group(1)+"-"+ymd.group(2)+"-"+ymd.group(3);Matcher dmy=DATE_DMY.matcher(s);if(dmy.find())return toIso(dmy.group(1),dmy.group(2),dmy.group(3));Matcher dm=DATE_DM.matcher(s);if(dm.find())return String.format(Locale.US,"%04d-%02d-%02d",inferredYear,Integer.parseInt(dm.group(2)),Integer.parseInt(dm.group(1)));return "";}
    private static String toIso(String dd,String mm,String yy){int y=Integer.parseInt(yy);if(y<100)y+=y>=70?1900:2000;int m=Integer.parseInt(mm),d=Integer.parseInt(dd);if(m<1||m>12||d<1||d>31)return "";return String.format(Locale.US,"%04d-%02d-%02d",y,m,d);}
    private static boolean isBalanceLine(String desc){String f=fold(desc);return f.contains("saldo anterior")||f.equals("saldo")||f.startsWith("saldo ")||f.contains("saldo do dia")||f.contains("saldo disponivel")||f.contains("total do dia");}
    private static String tidyDescription(String s){return truncate(s==null?"":s.replaceAll("\\s+"," ").trim(),180);}
    private static String cleanRaw(String raw){return raw==null?"":raw.replace('\u0000',' ').replace("\r\n","\n").replace('\r','\n').trim();}
    private static String firstNonEmptyLine(String text){for(String s:text.split("\\r?\\n"))if(!s.trim().isEmpty())return s.trim();return "";}
    private static String firstNonEmpty(String... values){for(String v:values)if(v!=null&&!v.trim().isEmpty())return v.trim();return "";}
    private static String findGroup(String text,Pattern p,int group){Matcher m=p.matcher(text==null?"":text);return m.find()?m.group(group).trim():"";}
    private static String join(List<String> values,String sep){StringBuilder b=new StringBuilder();for(String v:values){if(b.length()>0)b.append(sep);b.append(v);}return b.toString();}
    private static int count(String s,char ch){int n=0;for(int i=0;i<s.length();i++)if(s.charAt(i)==ch)n++;return n;}
    private static String truncate(String s,int max){if(s==null)return "";return s.length()<=max?s:s.substring(0,max);}
    private static String fold(String s){String n=Normalizer.normalize(s==null?"":s,Normalizer.Form.NFD).replaceAll("\\p{M}+","");return n.toLowerCase(Locale.ROOT).trim();}
}
