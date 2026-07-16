# -*- coding: utf-8 -*-
"""UI-Regressionstests fuer die Chat-Sendeleiste (CleanOrgaCleaner).

Zwei gemeldete Bugs:
  UI01 - Tastatur schliesst nach dem Senden NICHT (man muss erst manuell den
         Haken/"Fertig" tippen). Erwartet: nach dem Senden ist die Tastatur zu.
  UI02 - Der Sende-Pfeil zaehlt erst beim ZWEITEN Tap. Erwartet: EIN Tap sendet.

WICHTIG: UI02 ist iOS-First-Responder-Verhalten. Auf Android nimmt der Button den
ersten Tap normalerweise an -> dieser Test ist auf Android voraussichtlich GRUEN,
auch auf dem fehlerhaften Build. Er dokumentiert die Erwartung; die echte
Verifikation von UI02 erfolgt am iOS-Build. UI01 reproduziert sich auf Android
und ist hier aussagekraeftig.

Voraussetzungen: siehe common.py (Emulator + Appium + Debug-APK).
Aufruf: python test_chat_ui.py
"""
import sys
import time
from appium.webdriver.common.appiumby import AppiumBy
from common import (treiber, login, navigiere, finde, finde_desc, edittexts,
                    django, screenshot, adb, PAKET, ACTIVITY, TEST_PREFIX,
                    Protokoll)


def _cleanup():
    django("from webinterface.models import ChatMessage; "
           f"ChatMessage.objects.using('property_1').filter(text__contains='{TEST_PREFIX}').delete()")


def db_count(text_teil):
    r = django("from webinterface.models import ChatMessage; "
               f"print(ChatMessage.objects.using('property_1').filter(text__contains='{text_teil}').count())")
    return int(r) if r.strip().isdigit() else -1


def app_bereit(d, timeout=40) -> bool:
    """Wartet, bis die (auto-eingeloggte) App die Today-Seite zeigt."""
    ende = time.time() + timeout
    while time.time() < ende:
        if (finde(d, 'Today', 2) is not None or finde(d, 'Start', 2) is not None
                or finde(d, 'Chat', 2) is not None):
            return True
        time.sleep(2)
    return False


def geh_zu_chat(d) -> bool:
    """Navigation ueber den Hamburger-Button (oben mitte) -> 'Chat'."""
    z = finde(d, 'Chat', 2)
    if z is not None:
        z.click(); time.sleep(3); return True
    # Hamburger ~ (0.56 * Breite, 0.1375 * Hoehe)
    g = d.get_window_size()
    d.tap([(int(g['width'] * 0.56), int(g['height'] * 0.1375))])
    time.sleep(2)
    z = finde(d, 'Chat', 5)
    if z is not None:
        z.click(); time.sleep(4); return True
    return False


def oeffne_admin_chat(d) -> bool:
    geh_zu_chat(d)
    screenshot(d, 'ui_chatliste')
    # In der Chat-Liste oeffnet ein 'Chat'-Button je Zeile den Chat (Admin = Instanz 0)
    if finde(d, 'Admin', 6) is None and finde(d, 'Chat', 4) is None:
        return False
    try:
        btn = d.find_element(
            AppiumBy.ANDROID_UIAUTOMATOR,
            'new UiSelector().className("android.widget.Button").textContains("Chat").instance(0)')
        btn.click()
        time.sleep(4)
        return True
    except Exception:
        # Fallback: Name direkt antippen
        ziel = finde(d, 'Admin', 4)
        if ziel is not None:
            ziel.click()
            time.sleep(4)
            return True
        return False


def tastatur_offen(d) -> bool:
    try:
        return bool(d.is_keyboard_shown())
    except Exception:
        return 'mInputShown=true' in adb('shell', 'dumpsys', 'input_method')


def sende_button(d):
    """Sende-Pfeil finden: erst per content-desc/Text 'Send', sonst letzter Button."""
    b = finde_desc(d, 'Send', 3) or finde(d, 'Send', 2)
    if b is not None:
        return b
    knoepfe = d.find_elements(AppiumBy.CLASS_NAME, 'android.widget.Button')
    return knoepfe[-1] if knoepfe else None


def tippe(d, text):
    felder = edittexts(d)
    if not felder:
        return False
    felder[-1].click()
    time.sleep(1)
    felder[-1].send_keys(text)
    time.sleep(1)
    return True


def main():
    log = Protokoll()
    _cleanup()
    d = treiber()
    try:
        adb('shell', 'am', 'force-stop', PAKET)
        adb('shell', 'am', 'start', '-n', f'{PAKET}/{ACTIVITY}')
        time.sleep(6)
        # App ist i.d.R. bereits als tom eingeloggt (Session bleibt); sonst Login
        bereit = app_bereit(d)
        if not bereit:
            bereit = login(d)
        log('T-READY', 'App bereit (tom eingeloggt)', bereit)

        if not oeffne_admin_chat(d):
            log('UI01', 'Admin-Chat oeffnen', False, 'nicht gefunden')
            log('UI02', 'Admin-Chat oeffnen', False, 'uebersprungen')
            return log.abschluss()

        # ---------- UI01: Tastatur schliesst nach dem Senden ----------
        if not tippe(d, f'{TEST_PREFIX} ui01-keyboard'):
            log('UI01', 'Textfeld nicht gefunden', False)
        else:
            vorher_offen = tastatur_offen(d)   # Vorbedingung: Tastatur ist auf
            b = sende_button(d)
            if b is not None:
                b.click()
            time.sleep(3)
            kb = tastatur_offen(d)
            screenshot(d, 'ui01_after_send')
            # Test besteht, wenn Tastatur nach dem Senden ZU ist
            detail = ('Vorbedingung: Tastatur war ' +
                      ('offen' if vorher_offen else 'ZU?') +
                      ' | nach Senden: ' + ('offen' if kb else 'zu'))
            log('UI01', 'Tastatur schliesst nach dem Senden', (not kb), detail)

        # ---------- UI02: EIN Tap sendet (iOS-spezifisch) ----------
        if not tippe(d, f'{TEST_PREFIX} ui02-firsttap'):
            log('UI02', 'Textfeld nicht gefunden', False)
        else:
            b = sende_button(d)
            if b is not None:
                b.click()   # NUR EIN Tap
            time.sleep(4)
            cnt = db_count('ui02-firsttap')
            screenshot(d, 'ui02_after_one_tap')
            log('UI02', 'Ein Tap auf Pfeil sendet (iOS-spezifisch, Android meist gruen)',
                cnt >= 1, f'DB-count={cnt}')

        return log.abschluss()
    finally:
        _cleanup()
        try:
            d.quit()
        except Exception:
            pass


if __name__ == '__main__':
    sys.exit(main())
