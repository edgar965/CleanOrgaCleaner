# -*- coding: utf-8 -*-
"""Echtzeit-Chat-Test (Vordergrund / WebSocket) fuer CleanOrgaCleaner am Emulator.

RT01: eine serverseitig als Admin injizierte Nachricht an tom erscheint LIVE
      (WebSocket-Push, ohne App-Neustart).
RT02: tom sendet an Admin -> landet am Server (DB-Count steigt).

Voraussetzungen siehe common.py (Emulator + Appium + aktueller Debug-APK).
Aufruf: python test_chat_realtime.py
"""
import sys
import time
from appium.webdriver.common.appiumby import AppiumBy
from common import (treiber, finde, finde_desc, edittexts, django, screenshot,
                    adb, login, PAKET, ACTIVITY, TEST_PREFIX, Protokoll)

ADMIN_ID = 11
TOM_ID = 9


def app_bereit(d, timeout=40) -> bool:
    ende = time.time() + timeout
    while time.time() < ende:
        if (finde(d, 'Today', 2) is not None or finde(d, 'Start', 2) is not None
                or finde(d, 'Chat', 2) is not None):
            return True
        time.sleep(2)
    return False


def geh_zu_chat(d) -> bool:
    z = finde(d, 'Chat', 2)
    if z is not None:
        z.click(); time.sleep(3); return True
    g = d.get_window_size()
    d.tap([(int(g['width'] * 0.56), int(g['height'] * 0.1375))])
    time.sleep(2)
    z = finde(d, 'Chat', 5)
    if z is not None:
        z.click(); time.sleep(4); return True
    return False


def oeffne_admin(d) -> bool:
    geh_zu_chat(d)
    try:
        b = d.find_element(
            AppiumBy.ANDROID_UIAUTOMATOR,
            'new UiSelector().className("android.widget.Button").textContains("Chat").instance(0)')
        b.click(); time.sleep(4); return True
    except Exception:
        z = finde(d, 'Admin', 4)
        if z is not None:
            z.click(); time.sleep(4); return True
    return False


def injiziere(text) -> str:
    code = (
        "from webinterface.models import ChatMessage, Cleaner; "
        "from channels.layers import get_channel_layer; from asgiref.sync import async_to_sync; "
        f"a=Cleaner.objects.using('property_1').get(id={ADMIN_ID}); "
        f"t=Cleaner.objects.using('property_1').get(id={TOM_ID}); "
        f"m=ChatMessage.objects.using('property_1').create(sender=a, receiver=t, text='{text}'); "
        "d={'id':m.id,'text':m.text,'is_mine':False,'from_admin':True,'sender':'Admin','sender_name':'Admin','cleaner_id':t.id,'timestamp':m.timestamp.isoformat()}; "
        "cl=get_channel_layer(); async_to_sync(cl.group_send)('cleaner_%d__p1'%t.id,{'type':'chat_message','message':d}); "
        "print(m.id)"
    )
    return django(code).strip()


def db_count(teil) -> int:
    r = django("from webinterface.models import ChatMessage; "
               f"print(ChatMessage.objects.using('property_1').filter(text__contains='{teil}').count())")
    return int(r) if r.strip().isdigit() else -1


def sende_button(d):
    b = finde_desc(d, 'Send', 3) or finde(d, 'Send', 2)
    if b is not None:
        return b
    k = d.find_elements(AppiumBy.CLASS_NAME, 'android.widget.Button')
    return k[-1] if k else None


def main():
    log = Protokoll()
    django(f"from webinterface.models import ChatMessage; ChatMessage.objects.using('property_1').filter(text__contains='{TEST_PREFIX}').delete()")
    d = treiber()
    try:
        adb('shell', 'am', 'force-stop', PAKET)
        adb('shell', 'am', 'start', '-n', f'{PAKET}/{ACTIVITY}')
        time.sleep(6)
        # Nach Neuinstallation ist die Session weg -> Login-Screen. Sonst schon eingeloggt.
        bereit = app_bereit(d, 15)
        if not bereit:
            bereit = login(d)          # prop=1, tom/tom
        log('T-READY', 'App bereit / eingeloggt (tom)', bereit)

        if not oeffne_admin(d):
            log('RT01', 'Admin-Chat oeffnen', False)
            log('RT02', 'Admin-Chat oeffnen', False, 'uebersprungen')
            return log.abschluss()

        time.sleep(4)  # WebSocket verbinden lassen

        # RT01: serverseitig injizierte Nachricht muss LIVE erscheinen
        txt = f'{TEST_PREFIX} rt01-live'
        mid = injiziere(txt)
        gefunden = False
        for _ in range(10):
            if finde(d, 'rt01-live', 2) is not None:
                gefunden = True
                break
            time.sleep(1)
        screenshot(d, 'rt01_live')
        log('RT01', f'Injizierte Admin-Nachricht live per WS (msg {mid})', gefunden)

        # RT02: tom sendet an Admin
        felder = edittexts(d)
        if felder:
            felder[-1].click(); time.sleep(1)
            felder[-1].send_keys(f'{TEST_PREFIX} rt02-send'); time.sleep(1)
            b = sende_button(d)
            if b is not None:
                b.click()
            time.sleep(4)
        cnt = db_count('rt02-send')
        screenshot(d, 'rt02_send')
        log('RT02', 'tom sendet an Admin -> am Server', cnt >= 1, f'DB-count={cnt}')

        return log.abschluss()
    finally:
        django(f"from webinterface.models import ChatMessage; ChatMessage.objects.using('property_1').filter(text__contains='{TEST_PREFIX}').delete()")
        try:
            d.quit()
        except Exception:
            pass


if __name__ == '__main__':
    sys.exit(main())
