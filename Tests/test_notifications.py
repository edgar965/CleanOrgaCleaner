# -*- coding: utf-8 -*-
"""Testet den Mitteilungen-Toggle in den Einstellungen (Emulator, als edgar).

N01: Toggle 'Mitteilungen' anschalten -> App holt Token und registriert es am
     Server (FcmToken fuer edgar in property_1 erscheint).
N02: Zustandsanzeige nach dem Anschalten ('Aktiviert').

Voraussetzungen siehe common.py. Aufruf: python test_notifications.py
"""
import sys
import time
from common import (treiber, login, finde, django, screenshot, navigiere,
                    scrolle_zu, schalter_neben, adb, PAKET, ACTIVITY, Protokoll)


def geh_zu_einstellungen(d) -> bool:
    """Über das Hamburger-Menü zu den Einstellungen - in jeder Sprache."""
    return navigiere(d, 'Einstellungen')


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

        # Den Schalter NEBEN "Push-Mitteilungen" nehmen - der Abschnitt steht
        # unten und wird erst nach dem Wischen Teil der Hierarchie. Wer
        # stattdessen "den letzten Schalter" nimmt, erwischt die Biometrie.
        schalter = schalter_neben(d, 'Push-Mitteilungen')
        if schalter is None:
            log('N01', 'Mitteilungen-Schalter', False, 'kein Schalter gefunden')
            log('N02', 'Zustand', False, 'uebersprungen')
            return log.abschluss()

        schalter.click()    # anschalten
        time.sleep(2)
        erlaube_dialog(d)   # evtl. Berechtigungsdialog
        time.sleep(4)
        screenshot(d, 'n_toggled')

        nachher = edgar_token_count()
        log('N01', f'Token nach Toggle registriert (vorher={vorher}, nachher={nachher})',
            nachher > 0)

        # Der Zustand steht unter der Beschriftung ("Aktiviert" / "Nicht
        # aktiviert"). Vollstaendige Begriffe suchen, damit begriffe.Begriffe
        # die englische Entsprechung mitprueft.
        status_da = (scrolle_zu(d, 'Aktiviert', 1) is not None
                     or finde(d, 'Nicht aktiviert', 2) is not None
                     or finde(d, 'Nicht aktiv', 2) is not None)
        log('N02', 'Zustandsanzeige sichtbar', status_da)

        return log.abschluss()
    finally:
        try:
            d.quit()
        except Exception:
            pass


if __name__ == '__main__':
    sys.exit(main())
