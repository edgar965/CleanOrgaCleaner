#!/usr/bin/env python3
"""Eine App-Store-Version anlegen, den Build zuordnen und einreichen.

TestFlight und App Store sind zwei getrennte Wege: ein Build kann in
TestFlight laufen und trotzdem nie im Store erscheinen. Fuer den Store
braucht es eine eigene Version, den zugeordneten Build, die Hinweise
"Neu in dieser Version" und eine Einreichung zur Pruefung.

Eingereicht wird ueber ``reviewSubmissions`` - der aeltere Weg
``appStoreVersionSubmissions`` ist abgekuendigt und antwortet je nach App
mit 403. Er bleibt als Rueckfallebene stehen.

Aufruf (alle Angaben aus der Umgebung):
    ASC_KEY_ID, ASC_ISSUER_ID, ASC_PRIVATE_KEY, ASC_BUNDLE_ID,
    ASC_VERSION, ASC_BUILD
    python appstore_einreichen.py [--nur-pruefen]
"""
import os
import sys
import time

import jwt
import requests


class AppStoreEinreichung:
    """Legt Version 'x.yz' an, haengt den Build daran und reicht sie ein."""

    BASIS = 'https://api.appstoreconnect.apple.com/v1/'
    PLATTFORM = 'IOS'
    #: Wie bei den Vorversionen: nach Apples Freigabe automatisch ausliefern.
    AUSLIEFERUNG = 'AFTER_APPROVAL'
    #: Zustaende, in denen eine Version noch bearbeitet werden darf.
    OFFEN = ('PREPARE_FOR_SUBMISSION', 'DEVELOPER_REJECTED', 'REJECTED',
             'METADATA_REJECTED', 'INVALID_BINARY')

    def __init__(self, key_id, issuer, schluessel, bundle_id, version,
                 build_nummer, hinweise, nur_pruefen=False):
        self.key_id = key_id
        self.issuer = issuer
        self.schluessel = schluessel
        self.bundle_id = bundle_id
        self.version = str(version)
        self.build_nummer = str(build_nummer)
        self.hinweise = hinweise
        self.nur_pruefen = nur_pruefen

    # ----- Zugang ----------------------------------------------------------

    def _kopf(self):
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

    def _senden(self, methode, pfad, nutzlast):
        antwort = requests.request(methode, self.BASIS + pfad,
                                   headers=self._kopf(), json=nutzlast,
                                   timeout=60)
        if antwort.status_code >= 300:
            raise SystemExit('%s %s -> %s: %s'
                             % (methode, pfad, antwort.status_code,
                                antwort.text[:400]))
        return antwort.json() if antwort.content else {}

    # ----- Bausteine -------------------------------------------------------

    def app_id(self):
        daten = self._holen('apps', **{'filter[bundleId]': self.bundle_id})
        if not daten.get('data'):
            raise SystemExit('Keine App mit der Bundle-Id %s' % self.bundle_id)
        return daten['data'][0]['id']

    def build_id(self, app_id):
        daten = self._holen('builds', **{'filter[app]': app_id,
                                         'filter[version]': self.build_nummer})
        if not daten.get('data'):
            raise SystemExit('Build %s liegt nicht bei Apple.' % self.build_nummer)
        build = daten['data'][0]
        stand = build['attributes'].get('processingState')
        if stand != 'VALID':
            raise SystemExit('Build %s ist %s - noch nicht einreichbar.'
                             % (self.build_nummer, stand))
        return build['id']

    def version_holen(self, app_id):
        """Vorhandene Version dieser Nummer, sonst None."""
        daten = self._holen('apps/%s/appStoreVersions' % app_id, **{'limit': 20})
        for ver in daten.get('data', []):
            if ver['attributes'].get('versionString') == self.version:
                return ver
        return None

    def version_anlegen(self, app_id):
        antwort = self._senden('POST', 'appStoreVersions', {'data': {
            'type': 'appStoreVersions',
            'attributes': {'platform': self.PLATTFORM,
                           'versionString': self.version,
                           'releaseType': self.AUSLIEFERUNG},
            'relationships': {'app': {'data': {'type': 'apps', 'id': app_id}}}}})
        return antwort['data']

    def build_zuordnen(self, version_id, build):
        self._senden('PATCH', 'appStoreVersions/%s/relationships/build' % version_id,
                     {'data': {'type': 'builds', 'id': build}})

    def hinweise_setzen(self, version_id):
        """"Neu in dieser Version" je Sprache - Apple verlangt den Text.

        Die uebrigen Angaben (Beschreibung, Schluesselwoerter, Bilder) uebernimmt
        Apple beim Anlegen aus der Vorversion; nur dieses Feld bleibt leer.
        """
        gesetzt = []
        pfad = 'appStoreVersions/%s/appStoreVersionLocalizations' % version_id
        for loc in self._holen(pfad, **{'limit': 20})['data']:
            sprache = loc['attributes'].get('locale')
            text = self.hinweise.get(sprache)
            if not text:
                continue
            self._senden('PATCH', 'appStoreVersionLocalizations/%s' % loc['id'],
                         {'data': {'type': 'appStoreVersionLocalizations',
                                   'id': loc['id'],
                                   'attributes': {'whatsNew': text}}})
            gesetzt.append(sprache)
        return gesetzt

    def einreichen(self, app_id, version_id):
        """Sammelvorgang anlegen, Version hineinlegen, absenden."""
        vorgang = self._senden('POST', 'reviewSubmissions', {'data': {
            'type': 'reviewSubmissions',
            'attributes': {'platform': self.PLATTFORM},
            'relationships': {'app': {'data': {'type': 'apps', 'id': app_id}}}}})
        vorgang_id = vorgang['data']['id']
        self._senden('POST', 'reviewSubmissionItems', {'data': {
            'type': 'reviewSubmissionItems',
            'relationships': {
                'reviewSubmission': {'data': {'type': 'reviewSubmissions',
                                              'id': vorgang_id}},
                'appStoreVersion': {'data': {'type': 'appStoreVersions',
                                             'id': version_id}}}}})
        self._senden('PATCH', 'reviewSubmissions/%s' % vorgang_id, {'data': {
            'type': 'reviewSubmissions', 'id': vorgang_id,
            'attributes': {'submitted': True}}})
        return vorgang_id

    # ----- Ablauf ----------------------------------------------------------

    def ausfuehren(self):
        app_id = self.app_id()
        build = self.build_id(app_id)
        print('App %s, Build %s ist VALID' % (app_id, self.build_nummer))

        version = self.version_holen(app_id)
        if version is None:
            if self.nur_pruefen:
                print('[Trockenlauf] wuerde Version %s anlegen' % self.version)
                return
            version = self.version_anlegen(app_id)
            print('Version %s angelegt' % self.version)
        else:
            stand = version['attributes'].get('appStoreState')
            print('Version %s ist vorhanden (%s)' % (self.version, stand))
            if stand not in self.OFFEN:
                raise SystemExit('Version %s ist %s - nicht mehr aenderbar.'
                                 % (self.version, stand))
        if self.nur_pruefen:
            print('[Trockenlauf] wuerde Build zuordnen, Hinweise setzen, einreichen')
            return

        self.build_zuordnen(version['id'], build)
        print('Build %s zugeordnet' % self.build_nummer)
        print('Hinweise gesetzt fuer: %s' % ', '.join(self.hinweise_setzen(version['id'])))
        print('Zur Pruefung eingereicht, Vorgang %s'
              % self.einreichen(app_id, version['id']))


#: "Neu in dieser Version" - was der Nutzer im Store liest.
HINWEISE = {
    'de-DE': ('Aufgaben haben jetzt einen Namen. Er lässt sich beim Anlegen '
              'eingeben und erscheint bei allen Beteiligten – beim Ersteller '
              'ebenso wie bei der Arbeitskraft, der die Aufgabe zugewiesen ist. '
              'Die Aufgabenart ist je Firma vorbelegt, und der Name ist beim '
              'Öffnen bereits markiert, sodass er sich direkt überschreiben '
              'lässt.\n\n'
              'Fotos einer Aufgabe kann jetzt nur noch ändern, wer die Aufgabe '
              'erstellt hat, sowie das Büro. Für alle anderen sind sie eine '
              'Anweisung und bleiben unverändert sichtbar.'),
    'en-US': ('Tasks now have a name. You can enter it when creating a task, '
              'and everyone involved sees it – the person who created it as '
              'well as the person it is assigned to. The task type is preset '
              'per company, and the name is selected when you open it, ready '
              'to be overwritten.\n\n'
              'Photos attached to a task can now only be changed by the person '
              'who created it and by the office. For everyone else they are an '
              'instruction and stay visible unchanged.'),
}


def main():
    fehlend = [n for n in ('ASC_KEY_ID', 'ASC_ISSUER_ID', 'ASC_PRIVATE_KEY',
                           'ASC_BUNDLE_ID', 'ASC_VERSION', 'ASC_BUILD')
               if not os.environ.get(n)]
    if fehlend:
        raise SystemExit('Fehlende Angaben: %s' % ', '.join(fehlend))
    AppStoreEinreichung(
        os.environ['ASC_KEY_ID'], os.environ['ASC_ISSUER_ID'],
        os.environ['ASC_PRIVATE_KEY'], os.environ['ASC_BUNDLE_ID'],
        os.environ['ASC_VERSION'], os.environ['ASC_BUILD'], HINWEISE,
        nur_pruefen='--nur-pruefen' in sys.argv).ausfuehren()


if __name__ == '__main__':
    main()
