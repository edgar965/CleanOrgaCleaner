# -*- coding: utf-8 -*-
"""Gemeinsame Helfer für alle Appium-Testsuiten von CleanOrgaCleaner.

Voraussetzungen (siehe README.md):
- Android-Emulator läuft (pixel_7_-_api_36, emulator-5554)
- Appium-Server auf 127.0.0.1:4723 mit UiAutomator2-Treiber
- Debug-APK MIT eingebetteten Assemblies installiert
  (dotnet build -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true)
- SSH-Zugang zu root@91.99.235.72 für Server-Verifikation (property_1)

Testzugang: Property 1, User tom / Passwort tom (Cleaner id 9). Admin id 11.
"""
import subprocess
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


# ---------------------------------------------------------------- adb / Netz
def adb(*args):
    return subprocess.run([ADB, *args], capture_output=True, text=True, timeout=60).stdout.strip()


def netz(an: bool):
    z = 'enable' if an else 'disable'
    adb('shell', 'svc', 'wifi', z)
    adb('shell', 'svc', 'data', z)
    time.sleep(3)


def app_laeuft() -> bool:
    if adb('shell', 'pidof', PAKET).strip():
        return True
    return PAKET in adb('shell', 'ps', '-A')


def app_neustart():
    adb('shell', 'am', 'force-stop', PAKET)
    time.sleep(1)
    adb('shell', 'am', 'start', '-n', f'{PAKET}/{ACTIVITY}')
    ende = time.time() + 25
    while time.time() < ende and not app_laeuft():
        time.sleep(1)
    time.sleep(3)


def home():
    adb('shell', 'input', 'keyevent', 'KEYCODE_HOME')
    time.sleep(3)


def kein_fatal() -> bool:
    log = adb('shell', 'logcat', '-d', '-b', 'crash')
    return not ('FATAL EXCEPTION' in log and PAKET in log)


# ---------------------------------------------------------------- Server (SSH)
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
        f"DJANGO_SETTINGS_MODULE=CleanOrga.settings.prod_settings PYTHONPATH=/var/www/cleanorga venv/bin/python -c \"{script}\" 2>/dev/null"
    )


# ---------------------------------------------------------------- Appium
def treiber():
    o = UiAutomator2Options()
    o.platform_name = 'Android'
    o.device_name = 'emulator-5554'
    o.app_package = PAKET
    o.app_activity = ACTIVITY
    o.no_reset = True
    o.auto_grant_permissions = True
    o.new_command_timeout = 300
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
    ende = time.time() + timeout
    while time.time() < ende:
        try:
            return d.find_element(AppiumBy.ANDROID_UIAUTOMATOR, f'new UiSelector().descriptionContains("{text}")')
        except Exception:
            time.sleep(1)
    return None


def edittexts(d):
    return d.find_elements(AppiumBy.CLASS_NAME, 'android.widget.EditText')


def oeffne_menue(d):
    """Hamburger-Menü im Header öffnen (App navigiert über Flyout-Overlay,
    nicht über eine untere Tab-Bar)."""
    g = d.get_window_size()
    d.tap([(int(g['width'] * 0.56), int(g['height'] * 0.14))])
    time.sleep(2)


def navigiere(d, menuepunkt) -> bool:
    """Über das Hamburger-Menü zu Today/Chat/Auftrag/Settings navigieren."""
    ziel = finde(d, menuepunkt, 3)
    if ziel is None:
        oeffne_menue(d)
        ziel = finde(d, menuepunkt, 5)
    if ziel is not None:
        ziel.click()
        time.sleep(4)
        return True
    return False


def login(d, prop='1', user='tom', pw='tom') -> bool:
    """Robuster Login mit Retry (nach Netzwechsel/Neustart flaky)."""
    for _ in range(3):
        if finde(d, 'Today', 4) is not None or finde(d, 'Chat', 3) is not None:
            return True
        felder = []
        ende = time.time() + 20
        while time.time() < ende:
            felder = edittexts(d)
            if len(felder) >= 3:
                break
            time.sleep(1)
        if len(felder) >= 3:
            felder[0].clear(); felder[0].send_keys(prop)
            felder[1].clear(); felder[1].send_keys(user)
            felder[2].clear(); felder[2].send_keys(pw)
            knopf = finde(d, 'Login', 5)
            if knopf:
                knopf.click()
            time.sleep(10)
            if finde(d, 'Today', 8) is not None:
                return True
        time.sleep(3)
    return finde(d, 'Today', 5) is not None


def screenshot(d, name):
    try:
        d.get_screenshot_as_file(rf'D:\Daten\CleanOrga\CleanOrgaCleaner\Tests\screenshots\{name}.png')
    except Exception:
        pass


# ---------------------------------------------------------------- Protokoll
class Protokoll:
    def __init__(self):
        self.zeilen = []

    def __call__(self, nr, titel, ok, detail=''):
        self.zeilen.append((nr, titel, bool(ok), detail))
        print(f'[{"PASS" if ok else "FAIL"}] {nr}: {titel}' + (f' - {detail}' if detail else ''), flush=True)

    def abschluss(self) -> int:
        print('\n=== ERGEBNIS ===', flush=True)
        b = sum(1 for _, _, ok, _ in self.zeilen if ok)
        for nr, t, ok, det in self.zeilen:
            print(f'{"PASS" if ok else "FAIL"}  {nr:10} {t}' + (f' [{det}]' if det else ''), flush=True)
        print(f'{b}/{len(self.zeilen)} bestanden', flush=True)
        return 0 if b == len(self.zeilen) else 1


def testnachrichten_loeschen():
    django(f"from webinterface.models import ChatMessage; ChatMessage.objects.using('property_1').filter(text__contains='{TEST_PREFIX}').delete()")
