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
import org.mariadb.jdbc.MariaDbConnection;
import org.mariadb.jdbc.UrlParser;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.Statement;
import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.Locale;
import java.util.Properties;
import java.util.UUID;
import java.util.concurrent.atomic.AtomicBoolean;

/**
 * Beta 0.5.0: pessoas/casal, vínculos de conta, compras no cartão e faturas detalhadas.
 * Executa no processo :mysql para manter JDBC fora da UI.
 */
public class HouseholdFinanceService extends Service {
    public static final String EXTRA_ACTION = "household_action";
    public static final String EXTRA_CONFIG = "household_config";
    public static final String EXTRA_PAYLOAD = "household_payload";
    public static final String EXTRA_RECEIVER = "household_receiver";

    public static final String ACTION_CONTEXT = "household_context";
    public static final String ACTION_ADD_ENTITY = "household_add_entity";
    public static final String ACTION_ADD_ACCOUNT = "household_add_account";
    public static final String ACTION_ADD_CARD = "household_add_card";
    public static final String ACTION_ADD_TRANSACTION = "household_add_transaction";
    public static final String ACTION_ADD_CARD_PURCHASE = "household_add_card_purchase";
    public static final String ACTION_CARD_INVOICE = "household_card_invoice";

    private static final int CODE_RESULT = 2;
    private static final long HARD_TIMEOUT_MS = 20000L;
    private final Handler mainHandler = new Handler(Looper.getMainLooper());

    @Override public IBinder onBind(Intent intent) { return null; }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        if (intent == null) { stopSelf(startId); return START_NOT_STICKY; }
        final String action = intent.getStringExtra(EXTRA_ACTION);
        final String configJson = intent.getStringExtra(EXTRA_CONFIG);
        final String payloadJson = intent.getStringExtra(EXTRA_PAYLOAD);
        final ResultReceiver receiver = intent.getParcelableExtra(EXTRA_RECEIVER);
        final AtomicBoolean delivered = new AtomicBoolean(false);

        final Runnable timeout = () -> {
            if (delivered.compareAndSet(false, true)) send(receiver, fail("A operação excedeu 20 segundos e foi encerrada."));
            stopSelf(startId);
            mainHandler.postDelayed(() -> Process.killProcess(Process.myPid()), 250L);
        };
        mainHandler.postDelayed(timeout, HARD_TIMEOUT_MS);

        Thread worker = new Thread(() -> {
            JSONObject out;
            try {
                JSONObject config = new JSONObject(configJson == null ? "{}" : configJson);
                JSONObject payload = new JSONObject(payloadJson == null ? "{}" : payloadJson);
                switch (action == null ? "" : action) {
                    case ACTION_CONTEXT: out = context(config); break;
                    case ACTION_ADD_ENTITY: out = addEntity(config, payload); break;
                    case ACTION_ADD_ACCOUNT: out = addAccount(config, payload); break;
                    case ACTION_ADD_CARD: out = addCard(config, payload); break;
                    case ACTION_ADD_TRANSACTION: out = addTransaction(config, payload); break;
                    case ACTION_ADD_CARD_PURCHASE: out = addCardPurchase(config, payload); break;
                    case ACTION_CARD_INVOICE: out = cardInvoice(config, payload); break;
                    default: out = fail("Operação familiar/financeira desconhecida.");
                }
            } catch (Throwable e) {
                out = fail(cleanThrowable(e));
            }
            if (delivered.compareAndSet(false, true)) {
                mainHandler.removeCallbacks(timeout);
                send(receiver, out);
                stopSelf(startId);
            }
        }, "GranaOk-Household-Worker");
        worker.setDaemon(true);
        worker.start();
        return START_NOT_STICKY;
    }

    private JSONObject context(JSONObject c) throws Exception {
        String p = prefix(c);
        JSONArray people = new JSONArray();
        JSONArray accounts = new JSONArray();
        JSONArray cards = new JSONArray();
        try (Connection conn = openConnection(c)) {
            ensureTables(conn, p);
            try (Statement st = conn.createStatement(); ResultSet rs = st.executeQuery(
                "SELECT id,name,COALESCE(entity_kind,'person'),COALESCE(partner_name,''),active FROM " + p + "people ORDER BY active DESC,name ASC")) {
                while (rs.next()) {
                    JSONObject x = new JSONObject();
                    x.put("id", rs.getLong(1)); x.put("name", rs.getString(2)); x.put("kind", rs.getString(3));
                    x.put("partner_name", rs.getString(4)); x.put("active", rs.getInt(5) == 1); people.put(x);
                }
            }
            try (Statement st = conn.createStatement(); ResultSet rs = st.executeQuery(
                "SELECT a.id,a.name,a.type,a.initial_balance,a.current_balance,a.active,COALESCE(a.bank_code,'other'),a.person_id,COALESCE(pe.name,'') " +
                    "FROM " + p + "accounts a LEFT JOIN " + p + "people pe ON pe.id=a.person_id ORDER BY a.active DESC,a.name ASC")) {
                while (rs.next()) {
                    JSONObject x = new JSONObject();
                    x.put("id", rs.getLong(1)); x.put("name", rs.getString(2)); x.put("type", rs.getString(3));
                    x.put("initial_balance", rs.getDouble(4)); x.put("current_balance", rs.getDouble(5)); x.put("active", rs.getInt(6) == 1);
                    x.put("bank_code", rs.getString(7)); long personId = rs.getLong(8); if (!rs.wasNull()) x.put("person_id", personId);
                    x.put("person_name", rs.getString(9)); accounts.put(x);
                }
            }
            String cardSql = "SELECT c.id,c.name,c.closing_day,c.due_day,c.limit_amount,c.active,c.person_id,COALESCE(pe.name,'')," +
                "COALESCE((SELECT SUM(cp.amount) FROM " + p + "card_purchases cp WHERE cp.card_id=c.id AND DATE_FORMAT(cp.due_date,'%Y-%m')=DATE_FORMAT(CURDATE(),'%Y-%m')),0) " +
                "FROM " + p + "cards c LEFT JOIN " + p + "people pe ON pe.id=c.person_id ORDER BY c.active DESC,c.name ASC";
            try (Statement st = conn.createStatement(); ResultSet rs = st.executeQuery(cardSql)) {
                while (rs.next()) {
                    JSONObject x = new JSONObject();
                    x.put("id", rs.getLong(1)); x.put("name", rs.getString(2)); x.put("closing_day", rs.getInt(3)); x.put("due_day", rs.getInt(4));
                    x.put("limit_amount", rs.getDouble(5)); x.put("active", rs.getInt(6) == 1); long personId = rs.getLong(7); if (!rs.wasNull()) x.put("person_id", personId);
                    x.put("person_name", rs.getString(8)); x.put("invoice_amount", rs.getDouble(9)); cards.put(x);
                }
            }
        }
        JSONObject out = ok(); out.put("people", people); out.put("accounts", accounts); out.put("cards", cards); return out;
    }

    private JSONObject addEntity(JSONObject c, JSONObject a) throws Exception {
        String p = prefix(c);
        String kind = "couple".equals(a.optString("kind")) ? "couple" : "person";
        String first = a.optString("name", "").trim();
        String partner = a.optString("partnerName", "").trim();
        String display = a.optString("displayName", "").trim();
        if (first.isEmpty()) throw new IllegalArgumentException("Informe o nome da pessoa.");
        if ("couple".equals(kind) && partner.isEmpty()) throw new IllegalArgumentException("Informe o nome da segunda pessoa do casal.");
        if (display.isEmpty()) display = "couple".equals(kind) ? first + " e " + partner : first;
        try (Connection conn = openConnection(c)) {
            ensureTables(conn, p);
            try (PreparedStatement ps = conn.prepareStatement("INSERT INTO " + p + "people(name,active,entity_kind,partner_name) VALUES(?,1,?,?)")) {
                ps.setString(1, display); ps.setString(2, kind);
                if (partner.isEmpty()) ps.setNull(3, java.sql.Types.VARCHAR); else ps.setString(3, partner);
                ps.executeUpdate();
            }
        }
        JSONObject out = ok(); out.put("kind", "entity"); out.put("message", "couple".equals(kind) ? "Casal cadastrado." : "Pessoa cadastrada."); return out;
    }

    private JSONObject addAccount(JSONObject c, JSONObject a) throws Exception {
        String p = prefix(c);
        String name = a.optString("name", "").trim();
        String type = a.optString("type", "checking").trim();
        String bankCode = a.optString("bankCode", "other").trim().toLowerCase(Locale.ROOT);
        BigDecimal initial = decimal(a.optString("initialBalance", "0"));
        BigDecimal current = decimal(a.optString("currentBalance", initial.toPlainString()));
        Long personId = positiveId(a, "personId");
        if (name.isEmpty()) throw new IllegalArgumentException("Informe o nome da conta.");
        if (!type.matches("[A-Za-z0-9_-]{1,30}")) type = "bank";
        if (!bankCode.matches("[a-z0-9_-]{1,30}")) bankCode = "other";
        try (Connection conn = openConnection(c)) {
            ensureTables(conn, p); validatePerson(conn, p, personId);
            try (PreparedStatement ps = conn.prepareStatement("INSERT INTO " + p + "accounts(person_id,name,type,initial_balance,current_balance,active,bank_code) VALUES(?,?,?,?,?,1,?)")) {
                if (personId == null) ps.setNull(1, java.sql.Types.BIGINT); else ps.setLong(1, personId);
                ps.setString(2, name); ps.setString(3, type); ps.setBigDecimal(4, initial); ps.setBigDecimal(5, current); ps.setString(6, bankCode); ps.executeUpdate();
            }
        }
        JSONObject out = ok(); out.put("kind", "account"); out.put("message", "Conta cadastrada."); return out;
    }

    private JSONObject addCard(JSONObject c, JSONObject a) throws Exception {
        String p = prefix(c);
        String name = a.optString("name", "").trim();
        int closing = a.optInt("closingDay", 1), due = a.optInt("dueDay", 10);
        BigDecimal limit = decimal(a.optString("limitAmount", "0"));
        Long personId = positiveId(a, "personId");
        if (name.isEmpty()) throw new IllegalArgumentException("Informe o nome do cartão.");
        if (closing < 1 || closing > 31 || due < 1 || due > 31) throw new IllegalArgumentException("Confira os dias de fechamento e vencimento.");
        try (Connection conn = openConnection(c)) {
            ensureTables(conn, p); validatePerson(conn, p, personId);
            try (PreparedStatement ps = conn.prepareStatement("INSERT INTO " + p + "cards(person_id,name,closing_day,due_day,limit_amount,active) VALUES(?,?,?,?,?,1)")) {
                if (personId == null) ps.setNull(1, java.sql.Types.BIGINT); else ps.setLong(1, personId);
                ps.setString(2, name); ps.setInt(3, closing); ps.setInt(4, due); ps.setBigDecimal(5, limit); ps.executeUpdate();
            }
        }
        JSONObject out = ok(); out.put("kind", "card"); out.put("message", "Cartão cadastrado."); return out;
    }

    private JSONObject addTransaction(JSONObject c, JSONObject a) throws Exception {
        String p = prefix(c);
        String type = "income".equals(a.optString("type")) ? "income" : "expense";
        String description = a.optString("description", "").trim();
        String category = a.optString("category", "Outros").trim();
        String dueDate = a.optString("dueDate", today()).trim();
        String observations = a.optString("observations", "").trim();
        BigDecimal total = decimal(a.optString("totalAmount", a.optString("amount", "0")));
        int installments = "expense".equals(type) ? a.optInt("installments", 1) : 1;
        Long personId = positiveId(a, "personId"), accountId = positiveId(a, "accountId");
        if (description.isEmpty()) throw new IllegalArgumentException("Informe a descrição do lançamento.");
        if (total.compareTo(BigDecimal.ZERO) <= 0) throw new IllegalArgumentException("Informe um valor maior que zero.");
        if (installments < 1 || installments > 120) throw new IllegalArgumentException("O parcelamento deve ter entre 1 e 120 parcelas.");
        Calendar first = parseDate(dueDate);
        try (Connection conn = openConnection(c)) {
            ensureTables(conn, p); validatePerson(conn, p, personId); validateAccount(conn, p, accountId);
            Long categoryId = categoryId(conn, p, category, type);
            String group = installments > 1 ? UUID.randomUUID().toString() : null;
            BigDecimal base = total.divide(new BigDecimal(installments), 2, RoundingMode.DOWN), allocated = BigDecimal.ZERO;
            conn.setAutoCommit(false);
            try {
                String sql = "INSERT INTO " + p + "transactions(person_id,account_id,category_id,type,description,amount,due_date,status,source,observations,installment_group,installment_number,installment_total) VALUES(?,?,?,?,?,?,?,'pending',?,?,?,?,?)";
                for (int i = 1; i <= installments; i++) {
                    BigDecimal amount = i == installments ? total.subtract(allocated) : base; allocated = allocated.add(amount);
                    Calendar date = (Calendar) first.clone(); date.add(Calendar.MONTH, i - 1);
                    String label = installments > 1 ? description + " (" + i + "/" + installments + ")" : description;
                    try (PreparedStatement ps = conn.prepareStatement(sql)) {
                        if (personId == null) ps.setNull(1, java.sql.Types.BIGINT); else ps.setLong(1, personId);
                        if (accountId == null) ps.setNull(2, java.sql.Types.BIGINT); else ps.setLong(2, accountId);
                        if (categoryId == null) ps.setNull(3, java.sql.Types.BIGINT); else ps.setLong(3, categoryId);
                        ps.setString(4, type); ps.setString(5, label); ps.setBigDecimal(6, amount);
                        ps.setString(7, formatDate(date)); ps.setString(8, installments > 1 ? "installment" : "manual"); ps.setString(9, observations);
                        if (group == null) ps.setNull(10, java.sql.Types.VARCHAR); else ps.setString(10, group);
                        ps.setInt(11, i); ps.setInt(12, installments); ps.executeUpdate();
                    }
                }
                conn.commit();
            } catch (Throwable e) { conn.rollback(); throw e; }
            finally { conn.setAutoCommit(true); }
        }
        JSONObject out = ok(); out.put("kind", "transaction"); out.put("message", installments > 1 ? "Lançamento criado em " + installments + " parcelas." : "Lançamento criado."); return out;
    }

    private JSONObject addCardPurchase(JSONObject c, JSONObject a) throws Exception {
        String p = prefix(c);
        long cardId = a.optLong("cardId", 0);
        String description = a.optString("description", "").trim();
        String category = a.optString("category", "Outros").trim();
        String purchaseDate = a.optString("purchaseDate", today()).trim();
        String observations = a.optString("observations", "").trim();
        BigDecimal total = decimal(a.optString("totalAmount", a.optString("amount", "0")));
        int installments = a.optInt("installments", 1);
        Long personId = positiveId(a, "personId");
        if (cardId <= 0) throw new IllegalArgumentException("Escolha um cartão de crédito.");
        if (description.isEmpty()) throw new IllegalArgumentException("Informe a descrição da compra.");
        if (total.compareTo(BigDecimal.ZERO) <= 0) throw new IllegalArgumentException("Informe um valor maior que zero.");
        if (installments < 1 || installments > 60) throw new IllegalArgumentException("O cartão aceita de 1 a 60 parcelas no GranaOk.");
        Calendar purchase = parseDate(purchaseDate);

        String firstInvoiceMonth;
        try (Connection conn = openConnection(c)) {
            ensureTables(conn, p);
            int closing, due;
            Long cardPerson = null;
            try (PreparedStatement ps = conn.prepareStatement("SELECT closing_day,due_day,person_id FROM " + p + "cards WHERE id=? AND active=1 LIMIT 1")) {
                ps.setLong(1, cardId);
                try (ResultSet rs = ps.executeQuery()) {
                    if (!rs.next()) throw new IllegalArgumentException("Cartão não encontrado ou inativo.");
                    closing = rs.getInt(1); due = rs.getInt(2); long cp = rs.getLong(3); if (!rs.wasNull()) cardPerson = cp;
                }
            }
            if (personId == null) personId = cardPerson;
            validatePerson(conn, p, personId);
            Long categoryId = categoryId(conn, p, category, "expense");
            Calendar firstDue = invoiceDueDate(purchase, closing, due);
            firstInvoiceMonth = new SimpleDateFormat("yyyy-MM", Locale.US).format(firstDue.getTime());
            String group = installments > 1 ? UUID.randomUUID().toString() : null;
            BigDecimal base = total.divide(new BigDecimal(installments), 2, RoundingMode.DOWN), allocated = BigDecimal.ZERO;
            conn.setAutoCommit(false);
            try {
                String sql = "INSERT INTO " + p + "card_purchases(card_id,person_id,category_id,description,purchase_date,amount,invoice_month,due_date,installment_group,installment_number,installment_total,observations) VALUES(?,?,?,?,?,?,?,?,?,?,?,?)";
                for (int i = 1; i <= installments; i++) {
                    BigDecimal amount = i == installments ? total.subtract(allocated) : base; allocated = allocated.add(amount);
                    Calendar dueDate = (Calendar) firstDue.clone(); dueDate.add(Calendar.MONTH, i - 1);
                    String invoiceRef = new SimpleDateFormat("yyyy-MM-01", Locale.US).format(dueDate.getTime());
                    try (PreparedStatement ps = conn.prepareStatement(sql)) {
                        ps.setLong(1, cardId); if (personId == null) ps.setNull(2, java.sql.Types.BIGINT); else ps.setLong(2, personId);
                        if (categoryId == null) ps.setNull(3, java.sql.Types.BIGINT); else ps.setLong(3, categoryId);
                        ps.setString(4, description); ps.setString(5, purchaseDate); ps.setBigDecimal(6, amount); ps.setString(7, invoiceRef); ps.setString(8, formatDate(dueDate));
                        if (group == null) ps.setNull(9, java.sql.Types.VARCHAR); else ps.setString(9, group);
                        ps.setInt(10, i); ps.setInt(11, installments); ps.setString(12, observations); ps.executeUpdate();
                    }
                    syncInvoice(conn, p, cardId, invoiceRef, formatDate(dueDate));
                }
                conn.commit();
            } catch (Throwable e) { conn.rollback(); throw e; }
            finally { conn.setAutoCommit(true); }
        }
        JSONObject out = ok(); out.put("kind", "card_purchase"); out.put("card_id", cardId); out.put("month", firstInvoiceMonth);
        out.put("message", installments > 1 ? "Compra incluída em " + installments + " faturas." : "Compra incluída na fatura."); return out;
    }

    private JSONObject cardInvoice(JSONObject c, JSONObject a) throws Exception {
        String p = prefix(c); long cardId = a.optLong("cardId", 0); String month = normalizeMonth(a.optString("month", currentMonth()));
        if (cardId <= 0) throw new IllegalArgumentException("Cartão inválido.");
        JSONArray rows = new JSONArray(); JSONObject card = new JSONObject(); double total = 0d; String dueDate = ""; String status = "open";
        try (Connection conn = openConnection(c)) {
            ensureTables(conn, p);
            try (PreparedStatement ps = conn.prepareStatement("SELECT c.id,c.name,c.closing_day,c.due_day,c.limit_amount,c.person_id,COALESCE(pe.name,'') FROM " + p + "cards c LEFT JOIN " + p + "people pe ON pe.id=c.person_id WHERE c.id=? LIMIT 1")) {
                ps.setLong(1, cardId); try (ResultSet rs = ps.executeQuery()) {
                    if (!rs.next()) throw new IllegalArgumentException("Cartão não encontrado.");
                    card.put("id", rs.getLong(1)); card.put("name", rs.getString(2)); card.put("closing_day", rs.getInt(3)); card.put("due_day", rs.getInt(4)); card.put("limit_amount", rs.getDouble(5));
                    long personId = rs.getLong(6); if (!rs.wasNull()) card.put("person_id", personId); card.put("person_name", rs.getString(7));
                }
            }
            String sql = "SELECT cp.id,cp.description,cp.purchase_date,cp.amount,cp.due_date,cp.installment_number,cp.installment_total,COALESCE(cat.name,'Outros'),COALESCE(cp.observations,''),COALESCE(pe.name,'') " +
                "FROM " + p + "card_purchases cp LEFT JOIN " + p + "categories cat ON cat.id=cp.category_id LEFT JOIN " + p + "people pe ON pe.id=cp.person_id " +
                "WHERE cp.card_id=? AND DATE_FORMAT(cp.due_date,'%Y-%m')=? ORDER BY cp.purchase_date ASC,cp.id ASC";
            try (PreparedStatement ps = conn.prepareStatement(sql)) {
                ps.setLong(1, cardId); ps.setString(2, month); try (ResultSet rs = ps.executeQuery()) {
                    while (rs.next()) {
                        JSONObject x = new JSONObject(); x.put("id", rs.getLong(1)); x.put("description", rs.getString(2)); x.put("purchase_date", rs.getString(3));
                        x.put("amount", rs.getDouble(4)); x.put("due_date", rs.getString(5)); x.put("installment_number", rs.getInt(6)); x.put("installment_total", rs.getInt(7));
                        x.put("category", rs.getString(8)); x.put("observations", rs.getString(9)); x.put("person_name", rs.getString(10)); rows.put(x);
                        total += rs.getDouble(4); if (dueDate.isEmpty()) dueDate = rs.getString(5);
                    }
                }
            }
            try (PreparedStatement ps = conn.prepareStatement("SELECT status,DATE_FORMAT(due_date,'%Y-%m-%d') FROM " + p + "card_invoices WHERE card_id=? AND DATE_FORMAT(reference_month,'%Y-%m')=? ORDER BY id ASC LIMIT 1")) {
                ps.setLong(1, cardId); ps.setString(2, month); try (ResultSet rs = ps.executeQuery()) { if (rs.next()) { status = rs.getString(1); if (dueDate.isEmpty()) dueDate = rs.getString(2); } }
            }
            if (dueDate.isEmpty()) dueDate = theoreticalDueDate(month, card.optInt("due_day", 10));
        }
        JSONObject out = ok(); out.put("card", card); out.put("month", month); out.put("rows", rows); out.put("total", total); out.put("due_date", dueDate); out.put("status", status);
        out.put("available_limit", Math.max(0d, card.optDouble("limit_amount", 0d) - total)); return out;
    }

    private void syncInvoice(Connection conn, String p, long cardId, String invoiceRef, String dueDate) throws Exception {
        BigDecimal total = BigDecimal.ZERO;
        try (PreparedStatement ps = conn.prepareStatement("SELECT COALESCE(SUM(amount),0) FROM " + p + "card_purchases WHERE card_id=? AND invoice_month=?")) {
            ps.setLong(1, cardId); ps.setString(2, invoiceRef); try (ResultSet rs = ps.executeQuery()) { if (rs.next()) total = rs.getBigDecimal(1); }
        }
        Long id = null;
        try (PreparedStatement ps = conn.prepareStatement("SELECT id FROM " + p + "card_invoices WHERE card_id=? AND reference_month=? ORDER BY id ASC LIMIT 1")) {
            ps.setLong(1, cardId); ps.setString(2, invoiceRef); try (ResultSet rs = ps.executeQuery()) { if (rs.next()) id = rs.getLong(1); }
        }
        if (id == null) {
            try (PreparedStatement ps = conn.prepareStatement("INSERT INTO " + p + "card_invoices(card_id,reference_month,due_date,amount,status) VALUES(?,?,?,?, 'open')")) {
                ps.setLong(1, cardId); ps.setString(2, invoiceRef); ps.setString(3, dueDate); ps.setBigDecimal(4, total); ps.executeUpdate();
            }
        } else {
            try (PreparedStatement ps = conn.prepareStatement("UPDATE " + p + "card_invoices SET due_date=?,amount=? WHERE id=?")) {
                ps.setString(1, dueDate); ps.setBigDecimal(2, total); ps.setLong(3, id); ps.executeUpdate();
            }
        }
    }

    private void ensureTables(Connection conn, String p) throws Exception {
        try (Statement st = conn.createStatement()) {
            st.execute("CREATE TABLE IF NOT EXISTS " + p + "people (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,name VARCHAR(120) NOT NULL,active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
            st.execute("CREATE TABLE IF NOT EXISTS " + p + "accounts (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,person_id BIGINT UNSIGNED NULL,name VARCHAR(120) NOT NULL,type VARCHAR(40) NOT NULL DEFAULT 'bank',initial_balance DECIMAL(14,2) NOT NULL DEFAULT 0,current_balance DECIMAL(14,2) NOT NULL DEFAULT 0,active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),KEY idx_person(person_id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
            st.execute("CREATE TABLE IF NOT EXISTS " + p + "categories (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,name VARCHAR(120) NOT NULL,kind VARCHAR(20) NOT NULL DEFAULT 'expense',active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),UNIQUE KEY uq_cat(name,kind)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
            st.execute("CREATE TABLE IF NOT EXISTS " + p + "transactions (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,person_id BIGINT UNSIGNED NULL,account_id BIGINT UNSIGNED NULL,category_id BIGINT UNSIGNED NULL,type VARCHAR(20) NOT NULL,description VARCHAR(255) NOT NULL,amount DECIMAL(14,2) NOT NULL,due_date DATE NOT NULL,paid_date DATE NULL,status VARCHAR(20) NOT NULL DEFAULT 'pending',source VARCHAR(30) NOT NULL DEFAULT 'manual',created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),KEY idx_due(due_date),KEY idx_type(type),KEY idx_cat(category_id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
            st.execute("CREATE TABLE IF NOT EXISTS " + p + "cards (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,person_id BIGINT UNSIGNED NULL,name VARCHAR(120) NOT NULL,closing_day INT NOT NULL DEFAULT 1,due_day INT NOT NULL DEFAULT 10,limit_amount DECIMAL(14,2) NOT NULL DEFAULT 0,active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
            st.execute("CREATE TABLE IF NOT EXISTS " + p + "card_invoices (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,card_id BIGINT UNSIGNED NOT NULL,reference_month DATE NOT NULL,due_date DATE NOT NULL,amount DECIMAL(14,2) NOT NULL DEFAULT 0,status VARCHAR(20) NOT NULL DEFAULT 'open',created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),KEY idx_invoice_due(due_date),KEY idx_card_ref(card_id,reference_month)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
            st.execute("CREATE TABLE IF NOT EXISTS " + p + "card_purchases (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,card_id BIGINT UNSIGNED NOT NULL,person_id BIGINT UNSIGNED NULL,category_id BIGINT UNSIGNED NULL,description VARCHAR(255) NOT NULL,purchase_date DATE NOT NULL,amount DECIMAL(14,2) NOT NULL,invoice_month DATE NOT NULL,due_date DATE NOT NULL,installment_group VARCHAR(64) NULL,installment_number INT NOT NULL DEFAULT 1,installment_total INT NOT NULL DEFAULT 1,observations LONGTEXT NULL,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),KEY idx_cp_card_due(card_id,due_date),KEY idx_cp_invoice(card_id,invoice_month)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
        }
        addColumn(conn, p + "people", "entity_kind", "VARCHAR(20) NOT NULL DEFAULT 'person'");
        addColumn(conn, p + "people", "partner_name", "VARCHAR(120) NULL");
        addColumn(conn, p + "accounts", "bank_code", "VARCHAR(40) NULL");
        addColumn(conn, p + "transactions", "observations", "LONGTEXT NULL");
        addColumn(conn, p + "transactions", "installment_group", "VARCHAR(64) NULL");
        addColumn(conn, p + "transactions", "installment_number", "INT NOT NULL DEFAULT 1");
        addColumn(conn, p + "transactions", "installment_total", "INT NOT NULL DEFAULT 1");
    }

    private void addColumn(Connection conn, String table, String column, String definition) throws Exception {
        try (Statement st = conn.createStatement()) { st.execute("ALTER TABLE " + table + " ADD COLUMN " + column + " " + definition); }
        catch (java.sql.SQLException e) { if (!("42S21".equals(e.getSQLState()) || e.getErrorCode() == 1060)) throw e; }
    }

    private Long categoryId(Connection conn, String p, String name, String kind) throws Exception {
        if (name == null || name.trim().isEmpty()) name = "Outros";
        try (PreparedStatement ins = conn.prepareStatement("INSERT IGNORE INTO " + p + "categories(name,kind,active) VALUES(?,?,1)")) { ins.setString(1, name); ins.setString(2, kind); ins.executeUpdate(); }
        try (PreparedStatement ps = conn.prepareStatement("SELECT id FROM " + p + "categories WHERE name=? AND kind=? LIMIT 1")) {
            ps.setString(1, name); ps.setString(2, kind); try (ResultSet rs = ps.executeQuery()) { return rs.next() ? rs.getLong(1) : null; }
        }
    }

    private void validatePerson(Connection conn, String p, Long id) throws Exception {
        if (id == null) return;
        try (PreparedStatement ps = conn.prepareStatement("SELECT id FROM " + p + "people WHERE id=? AND active=1 LIMIT 1")) { ps.setLong(1, id); try (ResultSet rs = ps.executeQuery()) { if (!rs.next()) throw new IllegalArgumentException("Pessoa/casal selecionado não está disponível."); } }
    }

    private void validateAccount(Connection conn, String p, Long id) throws Exception {
        if (id == null) return;
        try (PreparedStatement ps = conn.prepareStatement("SELECT id FROM " + p + "accounts WHERE id=? AND active=1 LIMIT 1")) { ps.setLong(1, id); try (ResultSet rs = ps.executeQuery()) { if (!rs.next()) throw new IllegalArgumentException("Conta selecionada não está disponível."); } }
    }

    private Calendar invoiceDueDate(Calendar purchase, int closingDay, int dueDay) {
        Calendar close = (Calendar) purchase.clone(); int closeDom = Math.min(closingDay, close.getActualMaximum(Calendar.DAY_OF_MONTH));
        if (purchase.get(Calendar.DAY_OF_MONTH) > closeDom) { close.add(Calendar.MONTH, 1); closeDom = Math.min(closingDay, close.getActualMaximum(Calendar.DAY_OF_MONTH)); }
        close.set(Calendar.DAY_OF_MONTH, closeDom);
        Calendar due = (Calendar) close.clone(); if (dueDay <= closingDay) due.add(Calendar.MONTH, 1);
        due.set(Calendar.DAY_OF_MONTH, Math.min(dueDay, due.getActualMaximum(Calendar.DAY_OF_MONTH))); return due;
    }

    private String theoreticalDueDate(String month, int dueDay) throws Exception {
        Calendar c = Calendar.getInstance(); c.setTime(new SimpleDateFormat("yyyy-MM-dd", Locale.US).parse(month + "-01"));
        c.set(Calendar.DAY_OF_MONTH, Math.min(Math.max(1, dueDay), c.getActualMaximum(Calendar.DAY_OF_MONTH))); return formatDate(c);
    }

    private Connection openConnection(JSONObject c) throws Exception {
        String host = c.optString("host", "").trim(); int port = c.optInt("port", 3306); String database = c.optString("database", "").trim();
        String user = c.optString("user", "").trim(); String password = c.optString("password", ""); boolean ssl = c.optBoolean("ssl", false);
        if (host.isEmpty() || database.isEmpty() || user.isEmpty()) throw new IllegalArgumentException("Configuração MySQL incompleta.");
        StringBuilder url = new StringBuilder("jdbc:mariadb://").append(host).append(':').append(port).append('/').append(database).append("?connectTimeout=5000&socketTimeout=8000&tcpKeepAlive=true");
        if (ssl) url.append("&useSsl=true&trustServerCertificate=true"); else url.append("&useSsl=false");
        Properties props = new Properties(); props.setProperty("user", user); props.setProperty("password", password);
        UrlParser parser = UrlParser.parse(url.toString(), props); if (parser == null || parser.getHostAddresses() == null) throw new IllegalArgumentException("Connection string MySQL inválida.");
        return MariaDbConnection.newConnection(parser, null);
    }

    private static Long positiveId(JSONObject o, String key) { long v = o.optLong(key, 0); return v > 0 ? v : null; }
    private static BigDecimal decimal(String value) { String v = value == null ? "0" : value.trim(); if (v.contains(",")) v = v.replace(".", "").replace(',', '.'); if (v.isEmpty()) v = "0"; return new BigDecimal(v); }
    private static Calendar parseDate(String date) throws Exception { Calendar c = Calendar.getInstance(); c.setLenient(false); c.setTime(new SimpleDateFormat("yyyy-MM-dd", Locale.US).parse(date)); return c; }
    private static String formatDate(Calendar c) { return new SimpleDateFormat("yyyy-MM-dd", Locale.US).format(c.getTime()); }
    private static String currentMonth() { return new SimpleDateFormat("yyyy-MM", Locale.US).format(new java.util.Date()); }
    private static String today() { return new SimpleDateFormat("yyyy-MM-dd", Locale.US).format(new java.util.Date()); }
    private static String normalizeMonth(String m) { return m != null && m.matches("\\d{4}-(0[1-9]|1[0-2])") ? m : currentMonth(); }
    private static String prefix(JSONObject c) { String p = c.optString("prefix", "granaok_"); return p.matches("[A-Za-z0-9_]{1,32}") ? p : "granaok_"; }
    private static JSONObject ok() { JSONObject o = new JSONObject(); try { o.put("ok", true); } catch (Throwable ignored) {} return o; }
    private static JSONObject fail(String message) { JSONObject o = new JSONObject(); try { o.put("ok", false); o.put("error", message); } catch (Throwable ignored) {} return o; }
    private static String cleanThrowable(Throwable e) { String m = e == null ? "Falha desconhecida" : e.getMessage(); if (m == null || m.trim().isEmpty()) m = e == null ? "Falha desconhecida" : e.getClass().getSimpleName(); m = m.replaceAll("(?i)password=[^&\\s]+", "password=***").replaceAll("(?i)(password|senha)[=:]\\s*[^\\s,;]+", "$1=***"); return m.length() > 500 ? m.substring(0, 500) : m; }
    private static void send(ResultReceiver receiver, JSONObject payload) { if (receiver == null) return; try { Bundle b = new Bundle(); b.putString("json", payload == null ? "{}" : payload.toString()); receiver.send(CODE_RESULT, b); } catch (Throwable ignored) {} }
}
