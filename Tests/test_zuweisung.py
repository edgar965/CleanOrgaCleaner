# -*- coding: utf-8 -*-
"""Testsuite: Aufgaben-Zuweisungen muessen ohne Neustart in der App erscheinen.

Anlass (09.08.2026): Eine im Web zugewiesene Aufgabe erschien auf dem iPhone
nicht, obwohl die Heute-Seite offen war. Chat-Nachrichten kamen zur gleichen
Zeit an - der Server meldete also korrekt, und die Verbindung schien zu leben.

Der Crash-Bericht der App zeigte die eigentliche Ursache:

    TaskScheduler.UnobservedTaskException
    ObjectDisposedException, System.Threading.SemaphoreSlim

WebSocketService und OfflineQueueService sind Singletons, gaben in Dispose()
aber ihre Sperre frei. Dispose() laeuft im Betrieb (Abmelden, Fenster-
Destroying); auf iOS endet der Prozess dabei nicht. Danach scheiterte JEDER
Verbindungsaufbau - die App bekam keine Aufgaben-Aenderungen mehr, und nur ein
Neustart half.

Die Faelle:
  TZ01  Zuweisung erscheint live, waehrend die Heute-Seite offen ist.
  TZ02  Entfernte Zuweisung verschwindet wieder.
  TZ03  Nach Abmelden und erneutem Anmelden gilt das immer noch - genau hier
        lag der Fehler, weil das Abmelden den Dienst unbrauchbar machte.

Aufruf: python test_zuweisung.py
"""
import sys
import time

from common import (ACTIVITY, PAKET, AppiumBy, adb, app_laeuft, django, finde,
                    kein_fatal, login, navigiere, netz, screenshot, treiber,
                    Protokoll)

TESTNAME = '[ZUWEISUNG-TEST]'
TOM_ID = 9


def aufgabe_anlegen(zunaechst_an=None):
    """Testaufgabe fuer heute anlegen, optional an jemand anderen zugewiesen.

    ``zunaechst_an=None`` laesst sie unzugewiesen; mit einer Cleaner-Id wird
    daraus ein Umweisen (der gemeldete Fall).
    """
    zuweisung = (f"t.assigned_cleaner_list.set([{zunaechst_an}]); "
                 if zunaechst_an else "")
    return django(
        "from datetime import date; "
        "from webinterface.models import CleaningTask; "
        f"CleaningTask.objects.using('property_1').filter(name='{TESTNAME}').delete(); "
        "t = CleaningTask.objects.using('property_1').create("
        f"name='{TESTNAME}', planned_date=date.today(), aufwand=1.0, status='assigned'); "
        + zuweisung +
        "print('ID', t.id)"
    ).strip()


def zuweisen(cleaner_ids):
    """Ueber denselben Endpunkt wie die Oberflaeche (/api/tasks/<id>/assign/)."""
    return django(
        "import json; "
        "from django.test import Client; "
        "from django.contrib.auth import get_user_model; "
        "from webinterface.models import CleaningTask; "
        f"t = CleaningTask.objects.using('property_1').get(name='{TESTNAME}'); "
        "c = Client(SERVER_NAME='cleanorga.com'); "
        "u = get_user_model().objects.using('property_1').filter(is_superuser=True).first(); "
        "c.force_login(u); "
        "s = c.session; s['property_id']=1; s.save(); "
        f"r = c.post(f'/api/tasks/{{t.id}}/assign/', data=json.dumps({{'cleaning': {cleaner_ids}}}), "
        "content_type='application/json'); "
        "print('STATUS', r.status_code)"
    ).strip()


def aufraeumen():
    django("from webinterface.models import CleaningTask; "
           f"CleaningTask.objects.using('property_1').filter(name='{TESTNAME}').delete()")


def andere_kraft():
    """Irgendeine andere aktive Arbeitskraft - fuer das Umweisen."""
    antwort = django(
        "from webinterface.models import Cleaner; "
        f"c = Cleaner.objects.using('property_1').exclude(id={TOM_ID}).filter(aktiv=True).first(); "
        "print('ID', c.id if c else 0)")
    teile = antwort.split()
    return int(teile[-1]) if teile and teile[-1].isdigit() else 0


def anzahl_aufgaben(d) -> int:
    """Aufgaben-Kacheln in der Tagesliste zaehlen.

    Ueber die Anzahl statt ueber einen Suchtext: Die Testaufgabe erscheint
    ohne Apartment nur mit ihrer Aufgabenart ("cleaning"), und die steht auch
    an echten Aufgaben des Testkontos - ein Textvergleich waere mehrdeutig.
    Die Knoepfe der Kopfleiste (Menue, Arbeitszeit) werden abgezogen.
    """
    kopfleiste = {'☰', 'Start', 'Finish', 'Beenden'}
    knoepfe = d.find_elements(AppiumBy.CLASS_NAME, 'android.widget.Button')
    anzahl = 0
    for knopf in knoepfe:
        try:
            if (knopf.text or '').strip() not in kopfleiste:
                anzahl += 1
        except Exception:
            pass
    return anzahl


def warte_auf_anzahl(d, soll, timeout=45) -> bool:
    """Wartet, bis die Liste die erwartete Anzahl Aufgaben zeigt.

    Grosszuegig: Die Liste darf ueber die Live-Meldung ODER ueber das
    regelmaessige Nachladen aktuell werden - ein Neustart der App dagegen
    zaehlt nicht, denn der laedt ohnehin alles neu.
    """
    ende = time.time() + timeout
    while time.time() < ende:
        if anzahl_aufgaben(d) == soll:
            return True
        time.sleep(3)
    return anzahl_aufgaben(d) == soll


def auf_heute_seite(d):
    navigiere(d, 'Today')
    time.sleep(2)


def main():
    print('=== Testsuite: Zuweisungen erscheinen ohne Neustart ===', flush=True)
    log = Protokoll()
    netz(True)
    aufraeumen()

    d = treiber()
    try:
        adb('shell', 'am', 'force-stop', PAKET)
        adb('shell', 'am', 'start', '-n', f'{PAKET}/{ACTIVITY}')
        time.sleep(6)
        log('TZ-LOGIN', 'Angemeldet (tom)', login(d))
        auf_heute_seite(d)

        # ---- TZ01: Umweisen, waehrend die Heute-Seite offen ist ----------
        vorbesitzer = andere_kraft()
        aufgabe_anlegen(zunaechst_an=vorbesitzer or None)
        time.sleep(3)                      # Ausgangszustand wirken lassen
        vorher = anzahl_aufgaben(d)

        zuweisen([TOM_ID])
        erschienen = warte_auf_anzahl(d, vorher + 1)
        log('TZ01', 'Umgewiesene Aufgabe erscheint ohne Neustart', erschienen,
            f'{vorher} -> {anzahl_aufgaben(d)} Aufgaben')
        screenshot(d, 'tz01_zuweisung')

        # ---- TZ02: Zuweisung entfernen -> Aufgabe verschwindet -----------
        zuweisen([])
        verschwunden = warte_auf_anzahl(d, vorher)
        log('TZ02', 'Entfernte Zuweisung verschwindet wieder', verschwunden,
            f'jetzt {anzahl_aufgaben(d)} Aufgaben (erwartet {vorher})')

        # ---- TZ03: Dasselbe nach Abmelden und erneutem Anmelden ----------
        # Der eigentliche Fehler: Das Abmelden rief Dispose() auf den
        # Singletons auf, die dabei ihre Sperre freigaben. Danach scheiterte
        # jeder Verbindungsaufbau mit ObjectDisposedException - die App
        # bekam bis zum Neustart keine Aenderungen mehr mit.
        abgemeldet = navigiere(d, 'Logout')
        if abgemeldet:
            ja = finde(d, 'Yes', 5)
            if ja is not None:
                ja.click()
            time.sleep(6)
        log('TZ03a', 'Abmelden erfolgreich', abgemeldet and login(d))

        auf_heute_seite(d)
        time.sleep(3)
        vor_zuweisung = anzahl_aufgaben(d)

        zuweisen([TOM_ID])
        wieder_da = warte_auf_anzahl(d, vor_zuweisung + 1)
        log('TZ03b', 'Zuweisung erscheint auch nach erneutem Anmelden', wieder_da,
            f'{vor_zuweisung} -> {anzahl_aufgaben(d)} Aufgaben')
        screenshot(d, 'tz03_nach_neuanmeldung')

        log('TZ04', 'Keine FATAL EXCEPTION im Durchlauf',
            app_laeuft() and kein_fatal())

    finally:
        try:
            d.quit()
        except Exception:
            pass
        aufraeumen()
        netz(True)

    sys.exit(log.abschluss())


if __name__ == '__main__':
    main()
