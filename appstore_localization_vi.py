"""Legt die vietnamesische Produktseite (vi) fuer die aktuelle iOS-Version an.

Die API-Zugangsdaten werden aus update_appstore_metadata.py gelesen, damit der
private Schluessel nicht ein zweites Mal im Projekt liegt.

Aufruf:
    python appstore_localization_vi.py            # nur lesen (Status + Sprachen)
    python appstore_localization_vi.py --anlegen  # vi anlegen bzw. aktualisieren
"""

import re
import sys
import time
from pathlib import Path

import jwt
import requests

APP_ID = "6757746774"
API = "https://api.appstoreconnect.apple.com/v1"
QUELLE = Path(__file__).with_name("update_appstore_metadata.py")


def zugangsdaten():
    text = QUELLE.read_text(encoding="utf-8")
    key_id = re.search(r'KEY_ID\s*=\s*"([^"]+)"', text).group(1)
    issuer = re.search(r'ISSUER_ID\s*=\s*"([^"]+)"', text).group(1)
    key = re.search(r'PRIVATE_KEY\s*=\s*"""(.*?)"""', text, re.S).group(1).strip()
    return key_id, issuer, key


def kopfzeilen():
    key_id, issuer, key = zugangsdaten()
    now = int(time.time())
    token = jwt.encode(
        {"iss": issuer, "iat": now, "exp": now + 1200, "aud": "appstoreconnect-v1"},
        key,
        algorithm="ES256",
        headers={"kid": key_id, "typ": "JWT"},
    )
    return {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}


BESCHREIBUNG_VI = """CleanOrga - Ứng dụng lịch dọn dẹp thông minh cho doanh nghiệp, khách sạn và căn hộ cho thuê

Không còn tranh cãi về việc dọn dẹp! CleanOrga tự động phân công công việc vệ sinh trong doanh nghiệp, khách sạn và căn hộ cho thuê một cách công bằng.

TÍNH NĂNG CHÍNH:

Lịch dọn dẹp tự động
- Phân công công việc vệ sinh theo tuần
- Luân phiên công bằng giữa tất cả thành viên
- Lịch tổng quan với đầy đủ các mốc thời gian

Quản lý công việc
- Danh sách công việc chi tiết cho từng khu vực
- Đánh dấu hoàn thành chỉ với một chạm
- Theo dõi tiến độ theo thời gian thực

Tài liệu bằng hình ảnh
- Tải lên ảnh trước/sau khi dọn
- Ghi nhận công việc đã hoàn thành
- Minh bạch cho tất cả mọi người

Trao đổi
- Trò chuyện tích hợp giữa các thành viên
- Báo cáo sự cố trực tiếp
- Thống nhất nhanh khi có thắc mắc

Bảo mật
- Đăng nhập sinh trắc học (Face ID / Touch ID)
- Truyền dữ liệu an toàn
- Bảo vệ dữ liệu theo chuẩn GDPR

PHÙ HỢP VỚI:
- Đội ngũ vệ sinh
- Khách sạn và nhà nghỉ
- Căn hộ và nhà cho thuê
- Đơn vị quản lý bất động sản
- Nhà ở chung và không gian co-living
- Ký túc xá sinh viên

CleanOrga chấm dứt tình trạng quên việc và phân công không công bằng. Hãy tải ứng dụng và tận hưởng sự ngăn nắp không căng thẳng!"""

ATTRIBUTE_VI = {
    "description": BESCHREIBUNG_VI,
    "whatsNew": "Cải thiện độ ổn định và tốc độ đồng bộ dữ liệu.",
    "keywords": "dọn dẹp,vệ sinh,lịch,công việc,khách sạn,căn hộ,quản lý,nhân viên,phân công",
    "promotionalText": "Lịch dọn dẹp thông minh cho doanh nghiệp, khách sạn và căn hộ cho thuê.",
    "supportUrl": "https://schwanenburg.de/CleanOrga",
    "marketingUrl": "https://schwanenburg.de/CleanOrga",
}


def aktuelle_version(headers):
    r = requests.get(
        f"{API}/apps/{APP_ID}/appStoreVersions",
        headers=headers,
        params={"limit": 5},
        timeout=30,
    )
    r.raise_for_status()
    return r.json()["data"]


def lokalisierungen(headers, version_id):
    r = requests.get(
        f"{API}/appStoreVersions/{version_id}/appStoreVersionLocalizations",
        headers=headers,
        timeout=30,
    )
    r.raise_for_status()
    return r.json()["data"]


def main():
    headers = kopfzeilen()
    versionen = aktuelle_version(headers)

    ziel = None
    for v in versionen:
        a = v["attributes"]
        print(f"  Version {a['versionString']:<8} Status: {a['appStoreState']}   id={v['id']}")
        if ziel is None and a["appStoreState"] not in ("READY_FOR_SALE", "REPLACED_BY_NEW_VERSION"):
            ziel = v

    if ziel is None:
        print("\nKeine bearbeitbare Version gefunden.")
        return 1

    a = ziel["attributes"]
    print(f"\nZielversion: {a['versionString']} ({a['appStoreState']})")

    locs = lokalisierungen(headers, ziel["id"])
    vorhanden = {loc["attributes"]["locale"]: loc["id"] for loc in locs}
    print(f"Vorhandene Sprachen: {', '.join(sorted(vorhanden)) or '(keine)'}")

    if "--anlegen" not in sys.argv:
        print("\n(Nur gelesen. Zum Anlegen mit --anlegen aufrufen.)")
        return 0

    if "vi" in vorhanden:
        print("\nvi existiert bereits - aktualisiere ...")
        r = requests.patch(
            f"{API}/appStoreVersionLocalizations/{vorhanden['vi']}",
            headers=headers,
            json={
                "data": {
                    "type": "appStoreVersionLocalizations",
                    "id": vorhanden["vi"],
                    "attributes": ATTRIBUTE_VI,
                }
            },
            timeout=30,
        )
    else:
        print("\nLege vi an ...")
        r = requests.post(
            f"{API}/appStoreVersionLocalizations",
            headers=headers,
            json={
                "data": {
                    "type": "appStoreVersionLocalizations",
                    "attributes": {"locale": "vi", **ATTRIBUTE_VI},
                    "relationships": {
                        "appStoreVersion": {
                            "data": {"type": "appStoreVersions", "id": ziel["id"]}
                        }
                    },
                }
            },
            timeout=30,
        )

    print(f"HTTP {r.status_code}")
    if r.status_code not in (200, 201):
        print(r.text[:1200])
        return 1

    print("Erfolgreich.")

    # Status danach erneut pruefen - das Anlegen darf die laufende Pruefung nicht kippen
    kontrolle = requests.get(f"{API}/appStoreVersions/{ziel['id']}", headers=headers, timeout=30)
    if kontrolle.status_code == 200:
        neu = kontrolle.json()["data"]["attributes"]["appStoreState"]
        print(f"Status der Version nach der Aenderung: {neu}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
