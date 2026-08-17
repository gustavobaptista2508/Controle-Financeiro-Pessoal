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
 * Beta 0.3.9: edição de contas/lancamentos, status, observações e filtros mensais.
 * Executa no processo :mysql para manter JDBC isolado da UI.
 */
public class FinanceManageService extends Service {
    public static final String EXTRA_ACTION = "manage_action";
    public static final String EXTRA_CONFIG = "manage_config";
    public static final String EXTRA_PAYLOAD = "manage_payload";
    public static final String EXTRA_RECEIVER = "manage_receiver";

    public static final String ACTION_LIST_TRANSACTIONS = "manage_list_transactions";
    public static final String ACTION_ADD_TRANSACTION = "manage_add_transaction";
    public static final String ACTION_UPDATE_TRANSACTION = "manage_update_transaction";
    public static final String ACTION_SET_STATUS = "manage_set_status";
    public static final String ACTION_UPDATE_ACCOUNT = "manage_update_account";

    private static final int CODE_RESULT = 2;
    private static final long HARD_TIMEOUT_MS = 18000L;
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
            if (delivered.compareAndSet(false, true)) send(receiver, fail("A operação excedeu 18 segundos e foi encerrada."));
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
                    case ACTION_LIST_TRANSACTIONS: out = listTransactions(config, payload); break;
                    case ACTION_ADD_TRANSACTION: out = addTransaction(config, payload); break;
                    case ACTION_UPDATE_TRANSACTION: out = updateTransaction(config, payload); break;
                    case ACTION_SET_STATUS: out = setStatus(config, payload); break;
                    case ACTION_UPDATE_ACCOUNT: out = updateAccount(config, payload); break;
                    default: out = fail("Operação de gestão desconhecida.");
                }
            } catch (Throwable e) {
                out = fail(cleanThrowable(e));
            }
            if (delivered.compareAndSet(false, true)) {
                mainHandler.removeCallbacks(timeout);
                send(receiver, out);
                stopSelf(startId);
            }
        }, "GranaOk-Manage-Worker");
        worker.setDaemon(true);
        worker.start();
        return START_NOT_STICKY;
    }

    private JSONObject listTransactions(JSONObject c, JSONObject f) throws Exception {
        String p = prefix(c);
        String month = normalizeMonth(f.optString("month", currentMonth()));
        String nextMonth = shiftMonth(month, 1);
        String type = normalizeTypeFilter(f.optString("type", ""));
        String status = normalizeStatusFilter(f.optString("status", ""));
        String category = f.optString("category", "").trim();
        String search = f.optString("search", "").trim();
        JSONArray rows = new JSONArray();
        JSONArray categories = new JSONArray();
        double income = 0d, expense = 0d;

        try (Connection conn = openConnection(c)) {
            ensureCore(conn, p);
            ensureTransactionColumns(conn, p);
            String effective = "CASE WHEN t.status='paid' THEN 'paid' WHEN t.status='overdue' THEN 'overdue' WHEN t.status='pending' AND t.due_date<CURDATE() THEN 'overdue' ELSE t.status END";
            String sql = "SELECT t.id,t.type,t.description,t.amount,DATE_FORMAT(t.due_date,'%Y-%m-%d')," +
                "CASE WHEN t.paid_date IS NULL THEN NULL ELSE DATE_FORMAT(t.paid_date,'%Y-%m-%d') END," +
                "t.status," + effective + " effective_status,COALESCE(cat.name,'Outros'),COALESCE(t.observations,'')," +
                "COALESCE(t.installment_number,1),COALESCE(t.installment_total,1),COALESCE(t.installment_group,''),COALESCE(t.source,'manual'),t.account_id " +
                "FROM " + p + "transactions t LEFT JOIN " + p + "categories cat ON cat.id=t.category_id " +
                "WHERE t.due_date>=? AND t.due_date<? " +
                "AND (?='' OR t.type=?) AND (?='' OR " + effective + "=?) " +
                "AND (?='' OR COALESCE(cat.name,'Outros')=?) " +
                "AND (?='' OR t.description LIKE ? OR COALESCE(t.observations,'') LIKE ?) " +
                "ORDER BY t.due_date ASC,t.id ASC";
            try (PreparedStatement ps = conn.prepareStatement(sql)) {
                ps.setString(1, month + "-01"); ps.setString(2, nextMonth + "-01");
                ps.setString(3, type); ps.setString(4, type);
                ps.setString(5, status); ps.setString(6, status);
                ps.setString(7, category); ps.setString(8, category);
                ps.setString(9, search); ps.setString(10, "%" + search + "%"); ps.setString(11, "%" + search + "%");
                try (ResultSet rs = ps.executeQuery()) {
                    while (rs.next()) {
                        JSONObject x = new JSONObject();
                        x.put("id", rs.getLong(1)); x.put("type", rs.getString(2)); x.put("description", rs.getString(3));
                        x.put("amount", rs.getDouble(4)); x.put("due_date", rs.getString(5)); x.put("paid_date", rs.getString(6));
                        x.put("status", rs.getString(7)); x.put("effective_status", rs.getString(8)); x.put("category", rs.getString(9));
                        x.put("observations", rs.getString(10)); x.put("installment_number", rs.getInt(11)); x.put("installment_total", rs.getInt(12));
                        x.put("installment_group", rs.getString(13)); x.put("source", rs.getString(14));
                        long accountId = rs.getLong(15); if (!rs.wasNull()) x.put("account_id", accountId);
                        rows.put(x);
                        if ("income".equals(rs.getString(2))) income += rs.getDouble(4); else expense += rs.getDouble(4);
                    }
                }
            }
            try (PreparedStatement ps = conn.prepareStatement(
                "SELECT DISTINCT COALESCE(cat.name,'Outros') FROM " + p + "transactions t LEFT JOIN " + p + "categories cat ON cat.id=t.category_id WHERE t.due_date>=? AND t.due_date<? ORDER BY 1")) {
                ps.setString(1, month + "-01"); ps.setString(2, nextMonth + "-01");
                try (ResultSet rs = ps.executeQuery()) { while (rs.next()) categories.put(rs.getString(1)); }
            }
        }
        JSONObject out = ok();
        out.put("month", month); out.put("rows", rows); out.put("categories", categories);
        out.put("count", rows.length()); out.put("income", income); out.put("expenses", expense);
        return out;
    }

    private JSONObject addTransaction(JSONObject c, JSONObject a) throws Exception {
        String p = prefix(c);
        String type = a.optString("type", "expense").trim();
        if (!"income".equals(type)) type = "expense";
        String description = a.optString("description", "").trim();
        String category = a.optString("category", "Outros").trim();
        String dueDate = a.optString("dueDate", today()).trim();
        String observations = a.optString("observations", "").trim();
        BigDecimal total = decimal(a.optString("totalAmount", a.optString("amount", "0")));
        int installments = "expense".equals(type) ? a.optInt("installments", 1) : 1;
        if (description.isEmpty()) throw new IllegalArgumentException("Informe a descrição do lançamento.");
        if (total.compareTo(BigDecimal.ZERO) <= 0) throw new IllegalArgumentException("Informe um valor maior que zero.");
        if (installments < 1 || installments > 120) throw new IllegalArgumentException("O parcelamento deve ter entre 1 e 120 parcelas.");
        Calendar first = parseDate(dueDate);

        try (Connection conn = openConnection(c)) {
            ensureCore(conn, p); ensureTransactionColumns(conn, p);
            Long categoryId = categoryId(conn, p, category, type);
            String group = installments > 1 ? UUID.randomUUID().toString() : null;
            BigDecimal base = total.divide(new BigDecimal(installments), 2, RoundingMode.DOWN);
            BigDecimal allocated = BigDecimal.ZERO;
            conn.setAutoCommit(false);
            try {
                String sql = "INSERT INTO " + p + "transactions(category_id,type,description,amount,due_date,status,source,observations,installment_group,installment_number,installment_total) VALUES(?,?,?,?,?,'pending',?,?,?,?,?)";
                for (int i=1;i<=installments;i++) {
                    BigDecimal amount = i==installments ? total.subtract(allocated) : base;
                    allocated = allocated.add(amount);
                    Calendar date=(Calendar)first.clone(); date.add(Calendar.MONTH,i-1);
                    String label = installments>1 ? description + " (" + i + "/" + installments + ")" : description;
                    try (PreparedStatement ps=conn.prepareStatement(sql)) {
                        if(categoryId==null) ps.setNull(1,java.sql.Types.BIGINT); else ps.setLong(1,categoryId);
                        ps.setString(2,type); ps.setString(3,label); ps.setBigDecimal(4,amount);
                        ps.setString(5,new SimpleDateFormat("yyyy-MM-dd",Locale.US).format(date.getTime()));
                        ps.setString(6,installments>1?"installment":"manual"); ps.setString(7,observations);
                        if(group==null) ps.setNull(8,java.sql.Types.VARCHAR); else ps.setString(8,group);
                        ps.setInt(9,i); ps.setInt(10,installments); ps.executeUpdate();
                    }
                }
                conn.commit();
            } catch(Throwable e) { conn.rollback(); throw e; }
            finally { conn.setAutoCommit(true); }
        }
        JSONObject out=ok(); out.put("message",installments>1?"Lançamento criado em "+installments+" parcelas.":"Lançamento criado."); return out;
    }

    private JSONObject updateTransaction(JSONObject c, JSONObject a) throws Exception {
        String p=prefix(c); long id=a.optLong("id",0);
        if(id<=0) throw new IllegalArgumentException("Lançamento inválido.");
        String type=a.optString("type","expense").trim(); if(!"income".equals(type)) type="expense";
        String description=a.optString("description","").trim();
        String category=a.optString("category","Outros").trim();
        String dueDate=a.optString("dueDate",today()).trim();
        String observations=a.optString("observations","").trim();
        String status=normalizeStatus(a.optString("status","pending"));
        BigDecimal amount=decimal(a.optString("amount","0"));
        if(description.isEmpty()) throw new IllegalArgumentException("Informe a descrição.");
        if(amount.compareTo(BigDecimal.ZERO)<=0) throw new IllegalArgumentException("Informe um valor maior que zero.");
        parseDate(dueDate);
        try(Connection conn=openConnection(c)) {
            ensureCore(conn,p); ensureTransactionColumns(conn,p);
            Long categoryId=categoryId(conn,p,category,type);
            String paidExpr="paid".equals(status)?"COALESCE(paid_date,CURDATE())":"NULL";
            String sql="UPDATE "+p+"transactions SET type=?,description=?,category_id=?,amount=?,due_date=?,status=?,paid_date="+paidExpr+",observations=? WHERE id=?";
            try(PreparedStatement ps=conn.prepareStatement(sql)) {
                ps.setString(1,type); ps.setString(2,description);
                if(categoryId==null) ps.setNull(3,java.sql.Types.BIGINT); else ps.setLong(3,categoryId);
                ps.setBigDecimal(4,amount); ps.setString(5,dueDate); ps.setString(6,status); ps.setString(7,observations); ps.setLong(8,id);
                if(ps.executeUpdate()==0) throw new IllegalArgumentException("Lançamento não encontrado.");
            }
        }
        JSONObject out=ok(); out.put("message","Lançamento atualizado."); return out;
    }

    private JSONObject setStatus(JSONObject c, JSONObject a) throws Exception {
        String p=prefix(c); long id=a.optLong("id",0); String status=normalizeStatus(a.optString("status","pending"));
        if(id<=0) throw new IllegalArgumentException("Lançamento inválido.");
        try(Connection conn=openConnection(c)) {
            ensureCore(conn,p); ensureTransactionColumns(conn,p);
            String paidExpr="paid".equals(status)?"COALESCE(paid_date,CURDATE())":"NULL";
            try(PreparedStatement ps=conn.prepareStatement("UPDATE "+p+"transactions SET status=?,paid_date="+paidExpr+" WHERE id=?")) {
                ps.setString(1,status); ps.setLong(2,id); if(ps.executeUpdate()==0) throw new IllegalArgumentException("Lançamento não encontrado.");
            }
        }
        JSONObject out=ok(); out.put("message","Status atualizado."); out.put("status",status); return out;
    }

    private JSONObject updateAccount(JSONObject c, JSONObject a) throws Exception {
        String p=prefix(c); long id=a.optLong("id",0);
        String name=a.optString("name","").trim(); String type=a.optString("type","checking").trim();
        String bank=a.optString("bankCode","other").trim().toLowerCase(Locale.ROOT);
        BigDecimal initial=decimal(a.optString("initialBalance","0")); BigDecimal current=decimal(a.optString("currentBalance",initial.toPlainString()));
        boolean active=a.optBoolean("active",true);
        if(id<=0) throw new IllegalArgumentException("Conta inválida."); if(name.isEmpty()) throw new IllegalArgumentException("Informe o nome da conta.");
        if(!type.matches("[A-Za-z0-9_-]{1,30}")) type="bank"; if(!bank.matches("[a-z0-9_-]{1,30}")) bank="other";
        try(Connection conn=openConnection(c)) {
            ensureCore(conn,p); ensureAccountColumns(conn,p);
            try(PreparedStatement ps=conn.prepareStatement("UPDATE "+p+"accounts SET name=?,type=?,initial_balance=?,current_balance=?,active=?,bank_code=? WHERE id=?")) {
                ps.setString(1,name); ps.setString(2,type); ps.setBigDecimal(3,initial); ps.setBigDecimal(4,current); ps.setInt(5,active?1:0); ps.setString(6,bank); ps.setLong(7,id);
                if(ps.executeUpdate()==0) throw new IllegalArgumentException("Conta não encontrada.");
            }
        }
        JSONObject out=ok(); out.put("message","Conta atualizada."); return out;
    }

    private void ensureCore(Connection conn,String p) throws Exception {
        try(Statement st=conn.createStatement()) {
            st.execute("CREATE TABLE IF NOT EXISTS "+p+"categories (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,name VARCHAR(120) NOT NULL,kind VARCHAR(20) NOT NULL DEFAULT 'expense',active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),UNIQUE KEY uq_cat(name,kind)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
            st.execute("CREATE TABLE IF NOT EXISTS "+p+"accounts (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,person_id BIGINT UNSIGNED NULL,name VARCHAR(120) NOT NULL,type VARCHAR(40) NOT NULL DEFAULT 'bank',initial_balance DECIMAL(14,2) NOT NULL DEFAULT 0,current_balance DECIMAL(14,2) NOT NULL DEFAULT 0,active TINYINT(1) NOT NULL DEFAULT 1,created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),KEY idx_person(person_id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
            st.execute("CREATE TABLE IF NOT EXISTS "+p+"transactions (id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,person_id BIGINT UNSIGNED NULL,account_id BIGINT UNSIGNED NULL,category_id BIGINT UNSIGNED NULL,type VARCHAR(20) NOT NULL,description VARCHAR(255) NOT NULL,amount DECIMAL(14,2) NOT NULL,due_date DATE NOT NULL,paid_date DATE NULL,status VARCHAR(20) NOT NULL DEFAULT 'pending',source VARCHAR(30) NOT NULL DEFAULT 'manual',created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,PRIMARY KEY(id),KEY idx_due(due_date),KEY idx_type(type),KEY idx_cat(category_id)) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");
        }
    }

    private void ensureTransactionColumns(Connection conn,String p) throws Exception {
        addColumn(conn,p+"transactions","observations","LONGTEXT NULL");
        addColumn(conn,p+"transactions","installment_group","VARCHAR(64) NULL");
        addColumn(conn,p+"transactions","installment_number","INT NOT NULL DEFAULT 1");
        addColumn(conn,p+"transactions","installment_total","INT NOT NULL DEFAULT 1");
    }
    private void ensureAccountColumns(Connection conn,String p) throws Exception { addColumn(conn,p+"accounts","bank_code","VARCHAR(40) NULL"); }
    private void addColumn(Connection conn,String table,String column,String definition) throws Exception {
        try(Statement st=conn.createStatement()) { st.execute("ALTER TABLE "+table+" ADD COLUMN "+column+" "+definition); }
        catch(java.sql.SQLException e) { if(!("42S21".equals(e.getSQLState())||e.getErrorCode()==1060)) throw e; }
    }

    private Long categoryId(Connection conn,String p,String name,String kind) throws Exception {
        if(name==null||name.trim().isEmpty()) name="Outros";
        try(PreparedStatement ins=conn.prepareStatement("INSERT IGNORE INTO "+p+"categories(name,kind,active) VALUES(?,?,1)")) { ins.setString(1,name); ins.setString(2,kind); ins.executeUpdate(); }
        try(PreparedStatement ps=conn.prepareStatement("SELECT id FROM "+p+"categories WHERE name=? AND kind=? LIMIT 1")) {
            ps.setString(1,name); ps.setString(2,kind); try(ResultSet rs=ps.executeQuery()) { return rs.next()?rs.getLong(1):null; }
        }
    }

    private Connection openConnection(JSONObject c) throws Exception {
        String host=c.optString("host","").trim(); int port=c.optInt("port",3306); String database=c.optString("database","").trim();
        String user=c.optString("user","").trim(); String password=c.optString("password",""); boolean ssl=c.optBoolean("ssl",false);
        if(host.isEmpty()||database.isEmpty()||user.isEmpty()) throw new IllegalArgumentException("Configuração MySQL incompleta.");
        StringBuilder url=new StringBuilder("jdbc:mariadb://").append(host).append(':').append(port).append('/').append(database).append("?connectTimeout=5000&socketTimeout=7000&tcpKeepAlive=true");
        if(ssl) url.append("&useSsl=true&trustServerCertificate=true"); else url.append("&useSsl=false");
        Properties props=new Properties(); props.setProperty("user",user); props.setProperty("password",password);
        UrlParser parser=UrlParser.parse(url.toString(),props); if(parser==null||parser.getHostAddresses()==null) throw new IllegalArgumentException("Connection string MySQL inválida.");
        return MariaDbConnection.newConnection(parser,null);
    }

    private static String normalizeStatus(String s) { if("paid".equals(s)||"overdue".equals(s)) return s; return "pending"; }
    private static String normalizeStatusFilter(String s) { if("paid".equals(s)||"pending".equals(s)||"overdue".equals(s)) return s; return ""; }
    private static String normalizeTypeFilter(String s) { return ("income".equals(s)||"expense".equals(s))?s:""; }
    private static String normalizeMonth(String m) { if(m!=null&&m.matches("\\d{4}-(0[1-9]|1[0-2])")) return m; return currentMonth(); }
    private static String currentMonth() { return new SimpleDateFormat("yyyy-MM",Locale.US).format(new java.util.Date()); }
    private static String today() { return new SimpleDateFormat("yyyy-MM-dd",Locale.US).format(new java.util.Date()); }
    private static String shiftMonth(String month,int delta) {
        try { Calendar c=Calendar.getInstance(); c.setTime(new SimpleDateFormat("yyyy-MM-dd",Locale.US).parse(month+"-01")); c.add(Calendar.MONTH,delta); return new SimpleDateFormat("yyyy-MM",Locale.US).format(c.getTime()); }
        catch(Exception e) { return currentMonth(); }
    }
    private static Calendar parseDate(String date) throws Exception { Calendar c=Calendar.getInstance(); c.setTime(new SimpleDateFormat("yyyy-MM-dd",Locale.US).parse(date)); return c; }
    private static BigDecimal decimal(String value) {
        String v=value==null?"0":value.trim(); if(v.contains(",")) v=v.replace(".","").replace(',','.'); if(v.isEmpty())v="0"; return new BigDecimal(v);
    }
    private static String prefix(JSONObject c) { String p=c.optString("prefix","granaok_"); return p.matches("[A-Za-z0-9_]{1,32}")?p:"granaok_"; }
    private static JSONObject ok() { JSONObject o=new JSONObject(); try{o.put("ok",true);}catch(Exception ignored){} return o; }
    private static JSONObject fail(String message) { JSONObject o=new JSONObject(); try{o.put("ok",false);o.put("error",message);}catch(Exception ignored){} return o; }
    private static String cleanThrowable(Throwable e) {
        String m=e==null?"Falha desconhecida":e.getMessage(); if(m==null||m.trim().isEmpty())m=e==null?"Falha desconhecida":e.getClass().getSimpleName();
        m=m.replaceAll("(?i)password=[^&\\s]+","password=***").replaceAll("(?i)(password|senha)[=:]\\s*[^\\s,;]+","$1=***"); return m.length()>500?m.substring(0,500):m;
    }
    private static void send(ResultReceiver receiver,JSONObject payload) { if(receiver==null)return; try{Bundle b=new Bundle();b.putString("json",payload==null?"{}":payload.toString());receiver.send(CODE_RESULT,b);}catch(Throwable ignored){} }
}
