package com.gustavobaptista.financasgw;

import android.app.AlarmManager;
import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;

import org.json.JSONArray;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.Calendar;
import java.util.List;

public final class ReminderScheduler {
    private static final String PREFS = "financas_gw_reminders";
    private static final String KEY_JSON = "payload";
    private static final String KEY_CODES = "codes";

    private ReminderScheduler() {}

    public static synchronized void sync(Context context, String json) {
        if (context == null || json == null) return;
        Context app = context.getApplicationContext();
        SharedPreferences prefs = app.getSharedPreferences(PREFS, Context.MODE_PRIVATE);
        prefs.edit().putString(KEY_JSON, json).apply();
        schedule(app, json);
    }

    public static synchronized void rescheduleStored(Context context) {
        if (context == null) return;
        SharedPreferences prefs = context.getApplicationContext().getSharedPreferences(PREFS, Context.MODE_PRIVATE);
        String json = prefs.getString(KEY_JSON, null);
        if (json != null && !json.isEmpty()) schedule(context.getApplicationContext(), json);
    }

    private static void schedule(Context context, String json) {
        try {
            cancelPrevious(context);
            JSONObject root = new JSONObject(json);
            if (!root.optBoolean("enabled", false)) return;
            JSONArray daysArray = root.optJSONArray("daysBefore");
            JSONArray items = root.optJSONArray("items");
            int hour = Math.max(0, Math.min(23, root.optInt("hour", 9)));
            if (daysArray == null || items == null) return;

            AlarmManager alarmManager = (AlarmManager) context.getSystemService(Context.ALARM_SERVICE);
            List<Integer> codes = new ArrayList<>();
            long now = System.currentTimeMillis();

            for (int i = 0; i < items.length(); i++) {
                JSONObject item = items.optJSONObject(i);
                if (item == null) continue;
                String id = item.optString("id", "item-" + i);
                String title = item.optString("title", "Vencimento próximo");
                String body = item.optString("body", "Há um pagamento próximo do vencimento.");
                String dueDate = item.optString("dueDate", "");
                String[] parts = dueDate.split("-");
                if (parts.length != 3) continue;
                int year = Integer.parseInt(parts[0]);
                int month = Integer.parseInt(parts[1]);
                int day = Integer.parseInt(parts[2]);

                for (int j = 0; j < daysArray.length(); j++) {
                    int before = Math.max(0, daysArray.optInt(j, 0));
                    Calendar calendar = Calendar.getInstance();
                    calendar.set(Calendar.YEAR, year);
                    calendar.set(Calendar.MONTH, month - 1);
                    calendar.set(Calendar.DAY_OF_MONTH, day);
                    calendar.set(Calendar.HOUR_OF_DAY, hour);
                    calendar.set(Calendar.MINUTE, 0);
                    calendar.set(Calendar.SECOND, 0);
                    calendar.set(Calendar.MILLISECOND, 0);
                    calendar.add(Calendar.DAY_OF_MONTH, -before);
                    long trigger = calendar.getTimeInMillis();
                    if (trigger <= now) {
                        if (before == 0 && isSameDay(calendar, Calendar.getInstance())) {
                            trigger = now + 60_000L;
                        } else {
                            continue;
                        }
                    }

                    int code = positiveHash(id + "|" + before + "|" + dueDate);
                    Intent intent = new Intent(context, ReminderReceiver.class);
                    intent.putExtra("notification_id", code);
                    intent.putExtra("title", title);
                    intent.putExtra("body", body + reminderSuffix(before));
                    PendingIntent pendingIntent = PendingIntent.getBroadcast(
                            context,
                            code,
                            intent,
                            PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE
                    );
                    alarmManager.setAndAllowWhileIdle(AlarmManager.RTC_WAKEUP, trigger, pendingIntent);
                    codes.add(code);
                }
            }

            JSONArray codesJson = new JSONArray();
            for (Integer code : codes) codesJson.put(code);
            context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit().putString(KEY_CODES, codesJson.toString()).apply();
        } catch (Throwable error) {
            android.util.Log.e("FinancasGW", "Falha ao agendar vencimentos", error);
        }
    }

    private static void cancelPrevious(Context context) {
        try {
            SharedPreferences prefs = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE);
            JSONArray codes = new JSONArray(prefs.getString(KEY_CODES, "[]"));
            AlarmManager alarmManager = (AlarmManager) context.getSystemService(Context.ALARM_SERVICE);
            for (int i = 0; i < codes.length(); i++) {
                int code = codes.optInt(i, -1);
                if (code < 0) continue;
                Intent intent = new Intent(context, ReminderReceiver.class);
                PendingIntent pendingIntent = PendingIntent.getBroadcast(
                        context,
                        code,
                        intent,
                        PendingIntent.FLAG_NO_CREATE | PendingIntent.FLAG_IMMUTABLE
                );
                if (pendingIntent != null) {
                    alarmManager.cancel(pendingIntent);
                    pendingIntent.cancel();
                }
            }
            prefs.edit().putString(KEY_CODES, "[]").apply();
        } catch (Throwable ignored) {
        }
    }

    private static boolean isSameDay(Calendar a, Calendar b) {
        return a.get(Calendar.YEAR) == b.get(Calendar.YEAR)
                && a.get(Calendar.DAY_OF_YEAR) == b.get(Calendar.DAY_OF_YEAR);
    }

    private static String reminderSuffix(int before) {
        if (before == 0) return " · vence hoje";
        if (before == 1) return " · vence amanhã";
        return " · vence em " + before + " dias";
    }

    private static int positiveHash(String value) {
        int hash = value.hashCode();
        return hash == Integer.MIN_VALUE ? 1 : Math.abs(hash);
    }
}
