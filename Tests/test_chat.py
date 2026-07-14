# -*- coding: utf-8 -*-
"""Automatisierte Chat-Tests für CleanOrgaCleaner (Android-Emulator + Appium).

Deckt die Fälle aus chat_testcases.md ab. Testzugang: Property 1 / tom / tom (id 9).
Kollege für Kollegen-Tests: aylin (id 3). Admin: id 11.

Server-Injektion (eingehende Nachricht an tom) über den Django-Channel-Layer,
gleicher Pfad wie ein echter Admin->Cleaner-Chat (Gruppe cleaner_9__p1).
Server-Verifikation + Aufräumen per SSH (Django-ORM, property_1).
"""
import subprocess
import sys
import time

ADB = r'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe'
PAKET = 'com.cleanorga.cleaner'
ACTIVITY = 'crc64872e68f2eafd0b30.MainActivity'
APPIUM_URL = 'http://127.0.0.1:4723'
SSH_ZIEL = 'root@91.99.235.72'
TEST_PREFIX = '[APPIUM-TEST]'
TOM_ID = 9
AYLIN_ID = 3
ADMIN_ID = 11

from appium import webdriver
from appium.options.android import UiAutomator2Options
from appium.webdriver.common.appiumby import AppiumBy

ergebnisse = []


def adb(*args):
    return subprocess.run([ADB, *args], capture_output=True, text=True, timeout=60).stdout.strip()


def netz(an: bool):
    z = 'enable' if an else 'disable'
    adb('shell', 'svc', 'wifi', z); adb('shell', 'svc', 'data', z)
    time.sleep(3)


def app_laeuft() -> bool:
    if adb('shell', 'pidof', PAKET).strip():
        return True
    return PAKET in adb('shell', 'ps', '-A')


def ssh(befehl: str) -> str:
    return subprocess.run(['ssh', SSH_ZIEL, befehl], capture_output=True, text=True, timeout=90).stdout.strip()


def django(code: str) -> str:
    script = (
        "import os, django; os.environ.setdefault('DJANGO_SETTINGS_MODULE','CleanOrga.settings.prod_settings'); "
        "django.setup(); from webinterface.db_router import set_current_property; set_current_property(1); " + code
    )
    return ssh(
        "cd /var/www/cleanorga && set -a && . ./.env && set +a && "
        f"DJANGO_SETTINGS_MODULE=CleanOrga.settings.prod_settings PYTHONPATH=/var/www/cleanorga venv/bin/python -c \"{script}\" 2>/dev/null"
    )


def injiziere_admin_nachricht(text: str) -> str:
    """Erzeugt eine Admin->tom ChatMessage und pusht sie über den Channel-Layer
    (wie ein echter Admin-Chat). Gibt die Message-ID zurück."""
    code = (
        "from webinterface.models import ChatMessage, Cleaner; "
        "from channels.layers import get_channel_layer; from asgiref.sync import async_to_sync; "
        f"a=Cleaner.objects.using('property_1').get(id={ADMIN_ID}); "
        f"t=Cleaner.objects.using('property_1').get(id={TOM_ID}); "
        f"m=ChatMessage.objects.using('property_1').create(sender=a, receiver=t, text='{text}'); "
        "d={'id':m.id,'text':m.text,'is_mine':False,'from_admin':True,'sender':'Admin','sender_name':'Admin','cleaner_id':t.id,'timestamp':m.timestamp.isoformat()}; "
        "cl=get_channel_layer(); "
        "async_to_sync(cl.group_send)('cleaner_%d__p1'%t.id, {'type':'chat_message','message':d}); "
        "print(m.id)"
    )
    return django(code).strip()


def treiber():
    o = UiAutomator2Options()
    o.platform_name = 'Android'; o.device_name = 'emulator-5554'
    o.app_package = PAKET; o.app_activity = ACTIVITY
    o.no_reset = True; o.auto_grant_permissions = True; o.new_command_timeout = 300
    return webdriver.Remote(APPIUM_URL, options=o)


def finde(d, text, timeout=15):
    ende = time.time() + timeout
    while time.time() < ende:
        try:
            return d.find_element(AppiumBy.ANDROID_UIAUTOMATOR, f'new UiSelector().textContains("{text}")')
        except Exception:
            time.sleep(1)
    return None


def finde_desc(d, text, timeout=8):
    """Tab-Buttons haben oft nur content-desc statt text."""
    ende = time.time() + timeout
    while time.time() < ende:
        try:
            return d.find_element(AppiumBy.ANDROID_UIAUTOMATOR, f'new UiSelector().descriptionContains("{text}")')
        except Exception:
            time.sleep(1)
    return None


def tab(d, name):
    return finde(d, name, 5) or finde_desc(d, name, 5)


def oeffne_menue(d):
    """Öffnet das Hamburger-Menü im Header (die App navigiert über ein
    Flyout-Overlay, nicht über eine untere Tab-Bar)."""
    # Der Menü-Button sitzt oben mittig im Header. Erst per Text/desc suchen,
    # sonst per Koordinate (Header-Höhe ~275/2000 -> skaliert).
    groesse = d.get_window_size()
    x = int(groesse['width'] * 0.56)
    y = int(groesse['height'] * 0.14)
    d.tap([(x, y)])
    time.sleep(2)


def navigiere(d, menuepunkt):
    """Über das Hamburger-Menü zu 'Chat'/'Today'/... navigieren."""
    ziel = finde(d, menuepunkt, 3)
    if ziel is None:
        oeffne_menue(d)
        ziel = finde(d, menuepunkt, 5)
    if ziel is not None:
        ziel.click(); time.sleep(4); return True
    return False


def oeffne_chat_mit(d, name):
    """Öffnet in der Chat-Liste den Chat mit einem Kontakt. In jeder Zeile
    steht rechts ein 'Chat'-Button - dieser (nicht der Name) öffnet den Chat.
    Admin ist der erste, Kollegen darunter."""
    navigiere(d, 'Chat')
    # Sicherstellen, dass die Liste da ist
    if finde(d, name, 6) is None:
        return False
    # Den ersten 'Chat'-Button klicken (Admin) bzw. bei Kollegen: Button in der
    # Zeile des Namens. Einfach: alle 'Chat'-Buttons, ersten für Admin nehmen.
    try:
        if name == 'Admin':
            btn = d.find_element(AppiumBy.ANDROID_UIAUTOMATOR,
                                 'new UiSelector().className("android.widget.Button").textContains("Chat").instance(0)')
        else:
            # Kollege: 'Chat'-Button in derselben Zeile wie der Name
            btn = d.find_element(AppiumBy.ANDROID_UIAUTOMATOR,
                                 f'new UiSelector().className("android.widget.Button").fromParent(new UiSelector().textContains("{name}"))')
        btn.click(); time.sleep(4)
        return True
    except Exception:
        return False


def protokoll(nr, titel, ok, detail=''):
    ergebnisse.append((nr, titel, ok, detail))
    print(f'[{"PASS" if ok else "FAIL"}] {nr}: {titel}' + (f' - {detail}' if detail else ''), flush=True)


def screenshot(d, name):
    try: d.get_screenshot_as_file(rf'D:\Daten\CleanOrga\CleanOrgaCleaner\Tests\chat_{name}.png')
    except Exception: pass


def login(d):
    # Robust + mit Retry: nach Netzwechsel/Neustart braucht der Login-Screen
    # Zeit, und ein einzelner Versuch ist flaky.
    for versuch in range(3):
        if finde(d, 'Today', 4) is not None or tab(d, 'Chat') is not None:
            return True
        # auf die 3 Eingabefelder warten
        felder = []
        ende = time.time() + 20
        while time.time() < ende:
            felder = d.find_elements(AppiumBy.CLASS_NAME, 'android.widget.EditText')
            if len(felder) >= 3:
                break
            time.sleep(1)
        if len(felder) >= 3:
            felder[0].clear(); felder[0].send_keys('1')
            felder[1].clear(); felder[1].send_keys('tom')
            felder[2].clear(); felder[2].send_keys('tom')
            knopf = finde(d, 'Login', 5)
            if knopf:
                knopf.click()
            time.sleep(10)
            if finde(d, 'Today', 8) is not None or tab(d, 'Chat') is not None:
                return True
        time.sleep(3)
    return finde(d, 'Today', 5) is not None or tab(d, 'Chat') is not None


def sende_text(d, text):
    felder = d.find_elements(AppiumBy.CLASS_NAME, 'android.widget.EditText')
    if not felder:
        return False
    felder[-1].send_keys(text)
    time.sleep(1)
    senden = finde_desc(d, 'Send', 3) or finde(d, 'Send', 3)
    if senden is None:
        knoepfe = d.find_elements(AppiumBy.CLASS_NAME, 'android.widget.Button')
        senden = knoepfe[-1] if knoepfe else None
    if senden:
        senden.click(); time.sleep(4); return True
    return False


def db_count(text_teil):
    r = django(f"from webinterface.models import ChatMessage; print(ChatMessage.objects.using('property_1').filter(text__contains='{text_teil}').count())")
    return int(r) if r.strip().isdigit() else -1


def main():
    print('=== Chat-Testsuite CleanOrgaCleaner ===', flush=True)
    netz(True)
    django(f"from webinterface.models import ChatMessage; ChatMessage.objects.using('property_1').filter(text__contains='{TEST_PREFIX}').delete()")

    d = treiber()
    try:
        adb('shell', 'am', 'force-stop', PAKET)
        adb('shell', 'am', 'start', '-n', f'{PAKET}/{ACTIVITY}'); time.sleep(6)
        ok = login(d)
        protokoll('T-LOGIN', 'Login tom/tom', ok)

        # CH01: Chat-Liste lädt
        navigiere(d, 'Chat')
        liste_ok = finde(d, 'Admin', 8) is not None
        protokoll('CH01', 'Chat-Liste lädt (Admin sichtbar)', liste_ok)
        screenshot(d, 'ch01')

        # CH02: Nachricht an Admin senden -> DB
        gesendet = False
        if oeffne_chat_mit(d, 'Admin'):
            txt = f'{TEST_PREFIX} an-admin-ch02'
            gesendet = sende_text(d, txt)
            time.sleep(3)
            cnt = db_count('an-admin-ch02')
            protokoll('CH02', f'Nachricht an Admin am Server (count={cnt})', cnt >= 1)
            # CH03: erscheint im Verlauf
            im_verlauf = finde(d, 'an-admin-ch02', 5) is not None
            protokoll('CH03', 'Gesendete Nachricht erscheint im Verlauf', im_verlauf)
            screenshot(d, 'ch02')
        else:
            protokoll('CH02', 'Nachricht an Admin', False, 'Admin-Chat nicht gefunden')
            protokoll('CH03', 'Verlauf', False, 'übersprungen')

        # CH04: eingehende Nachricht in Echtzeit (Vordergrund), Admin-Chat offen
        mid = injiziere_admin_nachricht(f'{TEST_PREFIX} echtzeit-ch04')
        time.sleep(6)
        echtzeit = finde(d, 'echtzeit-ch04', 8) is not None
        protokoll('CH04', f'Eingehende Admin-Nachricht in Echtzeit sichtbar (msg {mid})', echtzeit)
        screenshot(d, 'ch04')

        # CH10: App im Hintergrund -> Nachricht injizieren -> zurück -> nachgeladen
        adb('shell', 'input', 'keyevent', 'KEYCODE_HOME'); time.sleep(4)
        injiziere_admin_nachricht(f'{TEST_PREFIX} hintergrund-ch10')
        time.sleep(5)
        adb('shell', 'am', 'start', '-n', f'{PAKET}/{ACTIVITY}'); time.sleep(7)
        # ggf. zurück in den Admin-Chat
        oeffne_chat_mit(d, 'Admin')
        nachgeladen = finde(d, 'hintergrund-ch10', 10) is not None
        protokoll('CH10', 'Im Hintergrund verpasste Nachricht wird beim Öffnen nachgeladen', nachgeladen)
        screenshot(d, 'ch10')

        # CH07: offline senden -> reconnect -> DB
        netz(False); time.sleep(6)
        sende_text(d, f'{TEST_PREFIX} offline-ch07')
        time.sleep(3)
        ok_dlg = finde(d, 'OK', 3)
        if ok_dlg: ok_dlg.click()
        netz(True); time.sleep(20)
        cnt7 = db_count('offline-ch07')
        protokoll('CH07', f'Offline-Nachricht nach Reconnect am Server (count={cnt7})', cnt7 >= 1)

        # CH12: Netz-Flattern, Chat offen
        for _ in range(4):
            netz(False); time.sleep(3); netz(True); time.sleep(3)
        time.sleep(10)
        crash = adb('shell', 'logcat', '-d', '-b', 'crash')
        fatal = 'FATAL EXCEPTION' in crash and PAKET in crash
        protokoll('CH12', 'Netz-Flattern im Chat: App stabil, kein FATAL', app_laeuft() and not fatal)

    finally:
        try: d.quit()
        except Exception: pass
        netz(True)
        django(f"from webinterface.models import ChatMessage; ChatMessage.objects.using('property_1').filter(text__contains='{TEST_PREFIX}').delete()")

    print('\n=== ERGEBNIS ===', flush=True)
    b = sum(1 for _, _, ok, _ in ergebnisse if ok)
    for nr, t, ok, det in ergebnisse:
        print(f'{"PASS" if ok else "FAIL"}  {nr:9} {t}' + (f' [{det}]' if det else ''), flush=True)
    print(f'{b}/{len(ergebnisse)} bestanden', flush=True)
    sys.exit(0 if b == len(ergebnisse) else 1)


if __name__ == '__main__':
    main()
