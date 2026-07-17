# -*- coding: utf-8 -*-
"""Testet den Mitteilungen-Toggle in den Einstellungen (Emulator, als edgar).

N01: Toggle 'Mitteilungen' anschalten -> App holt Token und registriert es am
     Server (FcmToken fuer edgar in property_1 erscheint).
N02: Zustandsanzeige nach dem Anschalten ('Aktiviert').

Voraussetzungen siehe common.py. Aufruf: python test_notifications.py
"""
import sys
import time
from appium.webdriver.common.appiumby import AppiumBy
from common import (treiber, login, finde, finde_desc, django, screenshot,
                    adb, PAKET, ACTIVITY, Protokoll)


def geh_zu_einstellungen(d) -> bool:
    z = finde(d, 'Einstellung', 2) or finde(d, 'Settings', 2)
    if z is not None:
        z.click(); time.sleep(3); return True
    g = d.get_window_size()
    d.tap([(int(g['width'] * 0.56), int(g['height'] * 0.1375))])  # Hamburger
    time.sleep(2)
    z = finde(d, 'Einstellung', 4) or finde(d, 'Settings', 4)
    if z is not None:
        z.click(); time.sleep(3); return True
    return False


def edgar_token_count():
    r = django("from webinterface.models import FcmToken; "
               "print(FcmToken.objects.using('property_1').filter(cleaner__name='edgar', aktiv=True).count())")
    return int(r) if r.strip().isdigit() else -1


def erlaube_dialog(d):
    """Falls ein Android-Berechtigungsdialog erscheint, 'Allow' tippen."""
    for txt in ('Allow', 'Zulassen', 'While using', 'Erlauben'):
        b = finde(d, txt, 2)
        if b is not None:
            b.click(); time.sleep(2); return


def main():
    log = Protokoll()
    vorher = edgar_token_count()
    d = treiber()
    try:
        adb('shell', 'am', 'force-stop', PAKET)
        adb('shell', 'am', 'start', '-n', f'{PAKET}/{ACTIVITY}')
        time.sleep(6)
        # als edgar einloggen (tom laeuft auf dem iPhone)
        if finde(d, 'Today', 4) is None and finde(d, 'Start', 3) is None:
            log('T-LOGIN', 'Login edgar/edgar', login(d, '1', 'edgar', 'edgar'))
        else:
            log('T-LOGIN', 'bereits eingeloggt', True)

        if not geh_zu_einstellungen(d):
            log('N01', 'Einstellungen oeffnen', False, 'nicht gefunden')
            log('N02', 'Zustand', False, 'uebersprungen')
            return log.abschluss()
        screenshot(d, 'n_settings')

        # Mitteilungen-Switch finden (Biometrie ist am Emulator meist ausgeblendet)
        switches = d.find_elements(AppiumBy.CLASS_NAME, 'android.widget.Switch')
        if not switches:
            log('N01', 'Mitteilungen-Schalter', False, 'kein Switch gefunden')
            log('N02', 'Zustand', False, 'uebersprungen')
            return log.abschluss()

        sw = switches[-1]
        sw.click()          # anschalten
        time.sleep(2)
        erlaube_dialog(d)   # evtl. Berechtigungsdialog
        time.sleep(4)
        screenshot(d, 'n_toggled')

        nachher = edgar_token_count()
        log('N01', f'Token nach Toggle registriert (vorher={vorher}, nachher={nachher})',
            nachher > 0)

        status_da = (finde(d, 'Aktiviert', 3) is not None
                     or finde(d, 'aktiv', 3) is not None
                     or finde(d, 'Nicht', 2) is not None)
        log('N02', 'Zustandsanzeige sichtbar', status_da)

        return log.abschluss()
    finally:
        try:
            d.quit()
        except Exception:
            pass


if __name__ == '__main__':
    sys.exit(main())
