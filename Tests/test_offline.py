# -*- coding: utf-8 -*-
"""Automatisierte Offline-Tests für CleanOrgaCleaner (Android-Emulator + Appium).

Deckt die automatisierbaren Fälle aus offline_testcases.md ab (TC01-TC09, TC11).
Testzugang: Property 1 / tom / tom (offizieller Test-User, Sprache en).
Netzwerk-Simulation: adb svc wifi/data. Server-Verifikation per SSH (Django-ORM).

Aufruf: python appium_offline_tests.py
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

from appium import webdriver
from appium.options.android import UiAutomator2Options
from appium.webdriver.common.appiumby import AppiumBy

ergebnisse = []


def adb(*args):
    return subprocess.run([ADB, *args], capture_output=True, text=True, timeout=60).stdout.strip()


def netz(an: bool):
    zustand = 'enable' if an else 'disable'
    adb('shell', 'svc', 'wifi', zustand)
    adb('shell', 'svc', 'data', zustand)
    time.sleep(3)


def app_laeuft() -> bool:
    # pidof ist auf manchen Images unzuverlässig -> ps als Fallback
    if adb('shell', 'pidof', PAKET).strip():
        return True
    return PAKET in adb('shell', 'ps', '-A')


def warte_auf_start(timeout=25) -> bool:
    ende = time.time() + timeout
    while time.time() < ende:
        if app_laeuft():
            return True
        time.sleep(1)
    return False


def app_stop():
    adb('shell', 'am', 'force-stop', PAKET)
    time.sleep(1)


def app_start():
    adb('shell', 'am', 'start', '-n', f'{PAKET}/{ACTIVITY}')
    warte_auf_start()
    time.sleep(3)


def ssh(befehl: str) -> str:
    return subprocess.run(['ssh', SSH_ZIEL, befehl], capture_output=True, text=True, timeout=90).stdout.strip()


def django(code: str) -> str:
    """Django-ORM-Einzeiler auf dem Server (property_1)."""
    script = (
        "import os, django; os.environ.setdefault('DJANGO_SETTINGS_MODULE','CleanOrga.settings.prod_settings'); "
        "django.setup(); from webinterface.db_router import set_current_property; set_current_property(1); " + code
    )
    return ssh(
        "cd /var/www/cleanorga && set -a && . ./.env && set +a && "
        f"DJANGO_SETTINGS_MODULE=CleanOrga.settings.prod_settings PYTHONPATH=/var/www/cleanorga venv/bin/python -c \"{script}\""
    )


def treiber():
    opts = UiAutomator2Options()
    opts.platform_name = 'Android'
    opts.device_name = 'emulator-5554'
    opts.app_package = PAKET
    opts.app_activity = ACTIVITY
    opts.no_reset = True          # App-Daten (Login/Cache) zwischen Sessions behalten
    opts.auto_grant_permissions = True
    opts.new_command_timeout = 300
    return webdriver.Remote(APPIUM_URL, options=opts)


def finde_text(d, text, timeout=15):
    ende = time.time() + timeout
    while time.time() < ende:
        try:
            el = d.find_element(AppiumBy.ANDROID_UIAUTOMATOR, f'new UiSelector().textContains("{text}")')
            return el
        except Exception:
            time.sleep(1)
    return None


def finde_klasse(d, klasse, index, timeout=15):
    ende = time.time() + timeout
    while time.time() < ende:
        els = d.find_elements(AppiumBy.CLASS_NAME, klasse)
        if len(els) > index:
            return els[index]
        time.sleep(1)
    return None


def protokoll(nr, titel, ok, detail=''):
    ergebnisse.append((nr, titel, ok, detail))
    status = 'PASS' if ok else 'FAIL'
    print(f'[{status}] {nr}: {titel}' + (f' - {detail}' if detail else ''), flush=True)


def screenshot(d, name):
    try:
        d.get_screenshot_as_file(rf'D:\Daten\CleanOrga\CleanOrgaCleaner\Tests\shot_{name}.png')
    except Exception:
        pass


def oeffne_menue(d):
    """Hamburger-Menü im Header öffnen (Flyout-Overlay, keine Tab-Bar)."""
    g = d.get_window_size()
    d.tap([(int(g['width'] * 0.56), int(g['height'] * 0.14))])
    time.sleep(2)


def navigiere(d, menuepunkt):
    ziel = finde_text(d, menuepunkt, 3)
    if ziel is None:
        oeffne_menue(d)
        ziel = finde_text(d, menuepunkt, 5)
    if ziel is not None:
        ziel.click(); time.sleep(4); return True
    return False


def login(d):
    """Robuster Login mit Retry (nach Netzwechsel/Neustart flaky)."""
    for _ in range(3):
        if finde_text(d, 'Today', 4) is not None:
            return True
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
            knopf = finde_text(d, 'Login', 5)
            if knopf:
                knopf.click()
            time.sleep(10)
            if finde_text(d, 'Today', 8) is not None:
                return True
        time.sleep(3)
    return finde_text(d, 'Today', 5) is not None


def main():
    print('=== Offline-Testsuite CleanOrgaCleaner ===', flush=True)
    netz(True)
    app_stop()
    adb('shell', 'pm', 'clear', PAKET)  # frischer Zustand für TC02
    time.sleep(1)

    d = treiber()
    try:
        # ---- TC02: Offline-Kaltstart ohne Cache ----
        netz(False)
        app_stop(); app_start()
        felder = finde_klasse(d, 'android.widget.EditText', 2, timeout=20)
        lebt = app_laeuft()
        protokoll('TC02', 'Offline-Kaltstart ohne Cache: kein Crash, Login-Seite da',
                  lebt and felder is not None)
        # Login offline versuchen -> saubere Fehlermeldung, kein Crash
        if felder is not None:
            eintraege = d.find_elements(AppiumBy.CLASS_NAME, 'android.widget.EditText')
            eintraege[0].send_keys('1'); eintraege[1].send_keys('tom'); eintraege[2].send_keys('tom')
            knopf = finde_text(d, 'Login', 5)
            if knopf: knopf.click()
            time.sleep(10)
            lebt = app_laeuft()
            protokoll('TC02b', 'Offline-Login ohne Cache: App stabil', lebt)
            screenshot(d, 'tc02')

        # ---- Grundlage: Online-Login + Aufgaben laden (Cache füllen) ----
        netz(True)
        time.sleep(4)
        app_stop(); app_start()
        ok = login(d)
        protokoll('T-LOGIN', 'Online-Login tom/tom', ok)
        screenshot(d, 'login')
        time.sleep(5)  # today-data + Cache

        # ---- TC05/TC06-Vorbereitung: Arbeit starten (online zuruecksetzen) ----
        # Falls Arbeit schon laeuft: erst beenden (Finish + Yes)
        stopknopf = finde_text(d, 'Finish', timeout=3)
        if stopknopf:
            stopknopf.click(); time.sleep(2)
            ja = finde_text(d, 'Yes', 5)
            if ja: ja.click(); time.sleep(3)

        # ---- TC03: Online -> Offline, Inhalte bleiben ----
        netz(False)
        time.sleep(8)
        d.swipe(400, 600, 400, 1400, 600)  # Pull-to-Refresh
        time.sleep(8)
        lebt = app_laeuft()
        # Inhalt noch da? Entweder Aufgaben-Buttons oder "No tasks" - aber NICHT leer/Fehlerseite
        inhalt = finde_text(d, 'Start', 5) or finde_text(d, 'No tasks', 5) or finde_text(d, 'Today', 5)
        protokoll('TC03', 'Offline-Refresh: App stabil, Inhalt bleibt', lebt and inhalt is not None)
        screenshot(d, 'tc03')

        # ---- TC09a: Offline-Banner sichtbar ----
        banner = finde_text(d, 'Offline', timeout=20)
        protokoll('TC09a', 'Offline-Banner erscheint nach Netztrennung', banner is not None)

        # ---- TC04 + TC01: App-Neustart offline -> Cache-Login + Aufgaben ----
        app_stop(); app_start()
        time.sleep(8)
        lebt = app_laeuft()
        inhalt = finde_text(d, 'Start', 10) or finde_text(d, 'No tasks', 5) or finde_text(d, 'Finish', 5)
        protokoll('TC01/TC04', 'Offline-Kaltstart mit Cache: Offline-Login + Inhalt da (Cache nicht zerstoert)',
                  lebt and inhalt is not None)
        screenshot(d, 'tc04')

        # ---- TC05: Arbeitsbeginn offline ----
        startknopf = finde_text(d, 'Start', timeout=10)
        if startknopf:
            startknopf.click()
            time.sleep(4)
            hinweis = finde_text(d, 'Saved', 5) or finde_text(d, 'sync', 5) or finde_text(d, 'Finish', 5)
            lebt = app_laeuft()
            protokoll('TC05', 'Arbeitsbeginn offline: Hinweis/Queue, kein Crash', lebt and hinweis is not None)
            ok_knopf = finde_text(d, 'OK', 3)
            if ok_knopf:
                ok_knopf.click(); time.sleep(1)
        else:
            protokoll('TC05', 'Arbeitsbeginn offline', False, 'Start-Button nicht gefunden')
        screenshot(d, 'tc05')

        # ---- TC07: Chat offline an Admin ----
        chat_ok = navigiere(d, 'Chat')
        if chat_ok:
            admin_eintrag = finde_text(d, 'Admin', 10)
            if admin_eintrag:
                try:
                    btn = d.find_element(AppiumBy.ANDROID_UIAUTOMATOR,
                        'new UiSelector().className("android.widget.Button").textContains("Chat").instance(0)')
                    btn.click(); time.sleep(4)
                except Exception:
                    admin_eintrag = None
                eingaben = d.find_elements(AppiumBy.CLASS_NAME, 'android.widget.EditText')
                if eingaben:
                    testtext = f'{TEST_PREFIX} offline an admin'
                    eingaben[-1].send_keys(testtext)
                    senden = finde_text(d, 'Send', 5) or finde_text(d, '>', 3)
                    # Senden-Button ist evtl. ein Bild-Button - notfalls letztes Button-Element
                    if senden is None:
                        knoepfe = d.find_elements(AppiumBy.CLASS_NAME, 'android.widget.Button')
                        senden = knoepfe[-1] if knoepfe else None
                    if senden:
                        senden.click(); time.sleep(4)
                    lebt = app_laeuft()
                    protokoll('TC07a', 'Chat-Nachricht offline abgesetzt: App stabil', lebt)
                    screenshot(d, 'tc07')
                else:
                    protokoll('TC07a', 'Chat offline', False, 'Eingabefeld nicht gefunden')
            else:
                protokoll('TC07a', 'Chat offline', False, 'Admin-Chat nicht gefunden')
        else:
            protokoll('TC07a', 'Chat offline', False, 'Chat-Tab nicht gefunden')

        # ---- TC06 + TC07b: Reconnect -> Queue synct (Server-Verifikation) ----
        netz(True)
        time.sleep(25)  # Reconnect + Queue-Verarbeitung
        lebt = app_laeuft()
        protokoll('TC06a', 'Reconnect: App stabil', lebt)

        nachricht = django(
            "from webinterface.models import ChatMessage; "
            "print(ChatMessage.objects.using('property_1').filter(text__contains='[APPIUM-TEST]').count())"
        )
        protokoll('TC07b', f'Offline-Chatnachricht am Server angekommen (Count={nachricht})',
                  nachricht.strip().isdigit() and int(nachricht.strip()) >= 1)

        # ---- TC09b: Banner verschwindet nach Reconnect ----
        heute_tab = finde_text(d, 'Today', 10)
        if heute_tab:
            heute_tab.click(); time.sleep(5)
        banner_weg = finde_text(d, 'Offline', timeout=5) is None
        protokoll('TC09b', 'Offline-Banner verschwindet nach Reconnect', banner_weg)
        screenshot(d, 'tc09')

        # ---- TC11: Netz-Flattern (Connect-Race) ----
        for _ in range(5):
            netz(False); time.sleep(3)
            netz(True); time.sleep(3)
        time.sleep(20)
        lebt = app_laeuft()
        absturz = adb('shell', 'logcat', '-d', '-s', 'AndroidRuntime:E')
        fatal = 'FATAL EXCEPTION' in absturz and PAKET in absturz
        protokoll('TC11', 'Netz-Flattern 5x: App stabil, kein FATAL', lebt and not fatal)
        screenshot(d, 'tc11')

        # ---- Aufraeumen: Arbeit beenden (falls offen), Testnachrichten loeschen ----
        stopknopf = finde_text(d, 'Finish', 5)
        if stopknopf:
            stopknopf.click(); time.sleep(2)
            ja = finde_text(d, 'Yes', 5)
            if ja: ja.click(); time.sleep(3)
        geloescht = django(
            "from webinterface.models import ChatMessage; "
            "print(ChatMessage.objects.using('property_1').filter(text__contains='[APPIUM-TEST]').delete())"
        )
        print(f'Aufgeraeumt: {geloescht}', flush=True)

    finally:
        try:
            d.quit()
        except Exception:
            pass
        netz(True)

    print('\n=== ERGEBNIS ===', flush=True)
    bestanden = sum(1 for _, _, ok, _ in ergebnisse if ok)
    for nr, titel, ok, detail in ergebnisse:
        print(f'{"PASS" if ok else "FAIL"}  {nr:9} {titel}' + (f' [{detail}]' if detail else ''), flush=True)
    print(f'{bestanden}/{len(ergebnisse)} bestanden', flush=True)
    sys.exit(0 if bestanden == len(ergebnisse) else 1)


if __name__ == '__main__':
    main()
