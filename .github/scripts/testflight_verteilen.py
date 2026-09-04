#!/usr/bin/env python3
"""Einen frisch hochgeladenen Build in TestFlight an die Tester verteilen.

App Store Connect nimmt einen Upload entgegen und legt ihn ab - mehr nicht.
Ohne Zuweisung an eine Gruppe sieht ihn kein externer Tester; genau daran lag
es am 04.09.2026, als Build 187 fertig verarbeitet war und trotzdem niemand
die neue Fassung bekam.

Interne Gruppen brauchen nichts: sie haben ``hasAccessToAllBuilds`` und sehen
jeden Build von selbst. Externe Gruppen brauchen zweierlei - die Zuweisung
und eine Beta-Pruefung durch Apple.

Aufruf (alle Angaben aus der Umgebung):
    ASC_KEY_ID, ASC_ISSUER_ID, ASC_PRIVATE_KEY, ASC_BUNDLE_ID, ASC_BUILD
    python testflight_verteilen.py [--nur-pruefen]
"""
import os
import sys
import time

import jwt
import requests


class TestFlightVerteilung:
    """Wartet auf die Verarbeitung und gibt den Build fuer die Tester frei."""

    BASIS = 'https://api.appstoreconnect.apple.com/v1/'
    #: App Store Connect braucht fuer die Verarbeitung selten mehr als zehn
    #: Minuten; nach dieser Zeit ist etwas anderes im Argen.
    WARTEN_MAX_S = 1800
    WARTEN_TAKT_S = 60

    def __init__(self, key_id, issuer, schluessel, bundle_id, build_nummer,
                 nur_pruefen=False):
        self.key_id = key_id
        self.issuer = issuer
        self.schluessel = schluessel
        self.bundle_id = bundle_id
        self.build_nummer = str(build_nummer)
        self.nur_pruefen = nur_pruefen

    # ----- Zugang ----------------------------------------------------------

    def _kopf(self):
        """Frisches Token je Aufruf - der Lauf dauert laenger als ein Token."""
        jetzt = int(time.time())
        token = jwt.encode(
            {'iss': self.issuer, 'iat': jetzt, 'exp': jetzt + 1200,
             'aud': 'appstoreconnect-v1'},
            self.schluessel, algorithm='ES256',
            headers={'kid': self.key_id, 'typ': 'JWT'})
        return {'Authorization': 'Bearer ' + token,
                'Content-Type': 'application/json'}

    def _holen(self, pfad, **parameter):
        antwort = requests.get(self.BASIS + pfad, headers=self._kopf(),
                               params=parameter or None, timeout=60)
        antwort.raise_for_status()
        return antwort.json()

    # ----- Bausteine -------------------------------------------------------

    def app_id(self):
        daten = self._holen('apps', **{'filter[bundleId]': self.bundle_id})
        if not daten.get('data'):
            raise SystemExit('Keine App mit der Bundle-Id %s' % self.bundle_id)
        return daten['data'][0]['id']

    def build_abwarten(self, app_id):
        """Gibt den Build zurueck, sobald er verarbeitet ist."""
        frist = time.time() + self.WARTEN_MAX_S
        while True:
            daten = self._holen('builds', **{
                'filter[app]': app_id, 'filter[version]': self.build_nummer})
            if daten.get('data'):
                build = daten['data'][0]
                stand = build['attributes'].get('processingState')
                print('Build %s: %s' % (self.build_nummer, stand), flush=True)
                if stand == 'VALID':
                    return build
                if stand in ('INVALID', 'FAILED'):
                    raise SystemExit('Build %s ist %s - keine Verteilung.'
                                     % (self.build_nummer, stand))
            else:
                print('Build %s noch nicht sichtbar ...' % self.build_nummer,
                      flush=True)
            if time.time() > frist:
                raise SystemExit('Build %s war nach %d Minuten nicht fertig.'
                                 % (self.build_nummer, self.WARTEN_MAX_S // 60))
            time.sleep(self.WARTEN_TAKT_S)

    def externe_gruppen(self, app_id):
        daten = self._holen('betaGroups', **{'filter[app]': app_id, 'limit': 50})
        return [g for g in daten.get('data', [])
                if not g['attributes'].get('isInternalGroup')]

    def zuweisen(self, gruppe, build):
        antwort = requests.post(
            self.BASIS + 'betaGroups/%s/relationships/builds' % gruppe['id'],
            headers=self._kopf(),
            json={'data': [{'type': 'builds', 'id': build['id']}]}, timeout=60)
        if antwort.status_code < 300:
            return 'zugewiesen'
        # Schon zugewiesen ist kein Fehler - der Lauf soll wiederholbar sein.
        text = antwort.text
        if 'already' in text.lower() or antwort.status_code == 409:
            return 'war schon zugewiesen'
        return 'FEHLER %s: %s' % (antwort.status_code, text[:200])

    def zur_pruefung_einreichen(self, build):
        """Ohne Beta-Pruefung bekommt kein externer Tester den Build."""
        antwort = requests.post(
            self.BASIS + 'betaAppReviewSubmissions', headers=self._kopf(),
            json={'data': {'type': 'betaAppReviewSubmissions',
                           'relationships': {'build': {'data': {
                               'type': 'builds', 'id': build['id']}}}}},
            timeout=60)
        if antwort.status_code < 300:
            return 'zur Pruefung eingereicht'
        text = antwort.text.lower()
        if 'already' in text or antwort.status_code == 409:
            return 'lag bereits zur Pruefung'
        return 'FEHLER %s: %s' % (antwort.status_code, antwort.text[:200])

    # ----- Ablauf ----------------------------------------------------------

    def ausfuehren(self):
        app_id = self.app_id()
        build = self.build_abwarten(app_id)
        gruppen = self.externe_gruppen(app_id)
        if not gruppen:
            print('Keine externe Gruppe - interne Tester sehen den Build ohnehin.')
            return
        for gruppe in gruppen:
            name = gruppe['attributes'].get('name')
            if self.nur_pruefen:
                print('[Trockenlauf] wuerde "%s" zuweisen und einreichen' % name)
                continue
            print('Gruppe "%s": %s' % (name, self.zuweisen(gruppe, build)))
        if not self.nur_pruefen:
            print('Beta-Pruefung: %s' % self.zur_pruefung_einreichen(build))


def main():
    fehlend = [n for n in ('ASC_KEY_ID', 'ASC_ISSUER_ID', 'ASC_PRIVATE_KEY',
                           'ASC_BUNDLE_ID', 'ASC_BUILD') if not os.environ.get(n)]
    if fehlend:
        raise SystemExit('Fehlende Angaben: %s' % ', '.join(fehlend))
    TestFlightVerteilung(
        os.environ['ASC_KEY_ID'], os.environ['ASC_ISSUER_ID'],
        os.environ['ASC_PRIVATE_KEY'], os.environ['ASC_BUNDLE_ID'],
        os.environ['ASC_BUILD'],
        nur_pruefen='--nur-pruefen' in sys.argv).ausfuehren()


if __name__ == '__main__':
    main()
