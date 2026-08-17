package com.granaok.app;

import org.json.JSONArray;
import org.json.JSONObject;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.text.Normalizer;
import java.util.Calendar;
import java.util.HashSet;
import java.util.Locale;
import java.util.Set;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/** Parser especializado para extratos de conta corrente do Sicoob/SISBR. */
public final class SicoobStatementParser {
    private SicoobStatementParser() {}

    private static final Pattern DATE_LINE = Pattern.compile("^\\s*(\\d{2})[/-](\\d{2})(?:[/-](\\d{2,4}))?\\s+(.+?)\\s*$");
    private static final Pattern MONEY_AT_END = Pattern.compile("(?<!\\d)((?:\\d{1,3}(?:\\.\\d{3})+|\\d+),\\d{2})\\s*([CD])?\\s*$", Pattern.CASE_INSENSITIVE);
    private static final Pattern MONEY_ONLY = Pattern.compile("^\\s*((?:\\d{1,3}(?:\\.\\d{3})+|\\d+),\\d{2})\\s*([CD])?\\s*$", Pattern.CASE_INSENSITIVE);
    private static final Pattern PERIOD_YEAR = Pattern.compile("(?i)PER[ÍI]ODO\\s*:\\s*\\d{2}[/-]\\d{2}[/-](\\d{4})");
    private static final Pattern ANY_FULL_YEAR = Pattern.compile("\\b\\d{2}[/-]\\d{2}[/-](20\\d{2})\\b");

    public static boolean isSupported(String raw) {
        String f = fold(raw);
        return f.contains("sicoob") && f.contains("extrato conta corrente") && f.contains("historico de movimentacao");
    }

    public static JSONObject parse(String raw, String fileName) throws Exception {
        String text = clean(raw);
        String[] lines = text.split("\\n", -1);
        int year = statementYear(text);
        Set<Integer> consumedAmountLines = new HashSet<Integer>();
        Set<String> seen = new HashSet<String>();
        JSONArray rows = new JSONArray();

        for (int i = 0; i < lines.length && rows.length() < 800; i++) {
            Matcher dm = DATE_LINE.matcher(lines[i]);
            if (!dm.matches()) continue;

            String dd = dm.group(1), mm = dm.group(2), yy = dm.group(3);
            String description = tidy(dm.group(4));
            String amountRaw = null;
            String dc = "";

            Matcher inline = MONEY_AT_END.matcher(description);
            if (inline.find()) {
                amountRaw = inline.group(1);
                dc = safeUpper(inline.group(2));
                description = tidy(description.substring(0, inline.start()));
            }

            if (amountRaw == null) {
                MoneyToken previous = nearestStandaloneMoney(lines, i - 1, -1, 3, consumedAmountLines);
                if (previous != null) {
                    amountRaw = previous.amount;
                    dc = previous.dc;
                    consumedAmountLines.add(previous.index);
                }
            }

            if (amountRaw == null) {
                MoneyToken next = nearestStandaloneMoney(lines, i + 1, 1, 3, consumedAmountLines);
                if (next != null) {
                    amountRaw = next.amount;
                    dc = next.dc;
                    consumedAmountLines.add(next.index);
                }
            }

            if (dc.isEmpty()) dc = nearbyDebitCredit(lines, i);
            if (isBalanceLine(description) || amountRaw == null || description.isEmpty()) continue;

            BigDecimal amount = DocumentParser.parseFlexibleMoney(amountRaw);
            if (amount == null || amount.compareTo(BigDecimal.ZERO) <= 0) continue;
            amount = amount.abs().setScale(2, RoundingMode.HALF_UP);

            String type;
            if ("C".equals(dc)) type = "income";
            else if ("D".equals(dc)) type = "expense";
            else type = inferType(description);

            int rowYear = year;
            if (yy != null && !yy.trim().isEmpty()) {
                rowYear = Integer.parseInt(yy);
                if (rowYear < 100) rowYear += rowYear >= 70 ? 1900 : 2000;
            }
            String date = String.format(Locale.US, "%04d-%02d-%02d", rowYear, Integer.parseInt(mm), Integer.parseInt(dd));
            String key = date + "|" + fold(description) + "|" + type + "|" + amount.toPlainString();
            if (!seen.add(key)) continue;

            JSONObject row = new JSONObject();
            row.put("date", date);
            row.put("description", description);
            row.put("type", type);
            row.put("amount", amount.doubleValue());

            String observations = collectDetails(lines, i);
            if (!observations.isEmpty()) row.put("observations", observations);
            rows.put(row);
        }

        JSONObject out = new JSONObject();
        out.put("ok", true);
        out.put("kind", "statement");
        out.put("format", "Sicoob Conta Corrente");
        out.put("bank", "Sicoob");
        out.put("file_name", fileName == null ? "extrato" : fileName);
        out.put("rows", rows);
        out.put("count", rows.length());
        return out;
    }

    private static MoneyToken nearestStandaloneMoney(String[] lines, int start, int step, int maxNonEmpty, Set<Integer> consumed) {
        int nonEmpty = 0;
        for (int i = start; i >= 0 && i < lines.length && nonEmpty < maxNonEmpty; i += step) {
            String s = lines[i].trim();
            if (s.isEmpty()) continue;
            nonEmpty++;
            if (consumed.contains(i)) return null;
            Matcher m = MONEY_ONLY.matcher(s);
            if (m.matches()) return new MoneyToken(i, m.group(1), safeUpper(m.group(2)));
            if ("C".equalsIgnoreCase(s) || "D".equalsIgnoreCase(s)) continue;
            return null;
        }
        return null;
    }

    private static String nearbyDebitCredit(String[] lines, int lineIndex) {
        int nonEmpty = 0;
        for (int i = lineIndex + 1; i < lines.length && nonEmpty < 3; i++) {
            String s = lines[i].trim();
            if (s.isEmpty()) continue;
            nonEmpty++;
            if ("C".equalsIgnoreCase(s) || "D".equalsIgnoreCase(s)) return s.toUpperCase(Locale.ROOT);
            Matcher m = MONEY_ONLY.matcher(s);
            if (m.matches()) {
                String dc = safeUpper(m.group(2));
                if (!dc.isEmpty()) return dc;
                continue;
            }
            break;
        }
        return "";
    }

    private static String collectDetails(String[] lines, int startLine) {
        StringBuilder details = new StringBuilder();
        int captured = 0;
        for (int i = startLine + 1; i < lines.length && captured < 7; i++) {
            String s = lines[i].trim();
            if (s.isEmpty()) continue;
            if (DATE_LINE.matcher(lines[i]).matches()) break;
            String f = fold(s);
            if (f.equals("resumo") || f.equals("historico de movimentacao") || f.startsWith("data historico")) break;
            if ("C".equalsIgnoreCase(s) || "D".equalsIgnoreCase(s) || MONEY_ONLY.matcher(s).matches()) continue;
            if (details.length() > 0) details.append(" · ");
            details.append(tidy(s));
            captured++;
        }
        String result = details.toString();
        return result.length() > 500 ? result.substring(0, 500) : result;
    }

    private static String inferType(String description) {
        String f = fold(description);
        if (f.contains("cr.ted") || f.contains("cta salario") || f.contains("pix rec") || f.contains("recebimento") || f.contains("credito")) return "income";
        return "expense";
    }

    private static boolean isBalanceLine(String description) {
        String f = fold(description);
        return f.contains("saldo anterior") || f.startsWith("saldo bloq") || f.equals("saldo") || f.startsWith("saldo do dia") || f.contains("saldo disponivel") || f.contains("total do dia");
    }

    private static int statementYear(String text) {
        Matcher period = PERIOD_YEAR.matcher(text);
        if (period.find()) return Integer.parseInt(period.group(1));
        Matcher any = ANY_FULL_YEAR.matcher(text);
        if (any.find()) return Integer.parseInt(any.group(1));
        return Calendar.getInstance().get(Calendar.YEAR);
    }

    private static String clean(String raw) {
        return raw == null ? "" : raw.replace('\u0000', ' ').replace('\f', '\n').replace("\r\n", "\n").replace('\r', '\n').trim();
    }

    private static String tidy(String s) {
        if (s == null) return "";
        String x = s.replaceAll("\\s+", " ").trim();
        return x.length() > 180 ? x.substring(0, 180) : x;
    }

    private static String fold(String s) {
        String n = Normalizer.normalize(s == null ? "" : s, Normalizer.Form.NFD).replaceAll("\\p{M}+", "");
        return n.toLowerCase(Locale.ROOT).trim();
    }

    private static String safeUpper(String s) {
        return s == null ? "" : s.trim().toUpperCase(Locale.ROOT);
    }

    private static final class MoneyToken {
        final int index;
        final String amount;
        final String dc;
        MoneyToken(int index, String amount, String dc) {
            this.index = index;
            this.amount = amount;
            this.dc = dc == null ? "" : dc;
        }
    }
}
