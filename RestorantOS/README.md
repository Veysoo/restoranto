# RestaurantOS

Restoran yönetim sistemi: React + ASP.NET Core API + SQL Server (Docker).

---

## Günlük Başlatma

```bat
Startup.bat
```

Tarayıcı otomatik açılır.

---

## Giriş Bilgileri

| Kullanıcı | Şifre | Rol |
|-----------|-------|-----|
| **admin** | `Resto@Admin2024!` | Yönetici |
| ahmet | `Garson@2024!` | Garson |
| ayse | `Kasiyer@2024!` | Kasiyer |

---

## Erişim

| Cihaz | Adres |
|-------|-------|
| Bu bilgisayar | http://localhost:8080 |
| Ağdaki diğer PC | http://192.168.1.103:8080 |
| **Domain (PC)** | http://restaurantos.local *(Setup-Hosts.bat gerekli)* |
| **Mobil** | Giriş ekranındaki **QR kodu** tarayın |

### PC'de Domain Adı Aktif Etme (bir kez)

`Setup-Hosts.bat` dosyasına sağ tık → **Yönetici olarak çalıştır**

Bu işlem sonrası tarayıcıdan `http://restaurantos.local` yazarak erişebilirsiniz.

### Mobil Erişim

Telefonla giriş yapmak için: sunucu bilgisayarda `http://localhost:8080` adresini açın, giriş ekranındaki **QR kodu** telefonunuzla tarayın.

---

## Özellikler

- 🏠 Canlı kat planı, masa durumları
- 🛒 Sipariş alma, değiştirme
- 💰 Ödeme (nakit, kredi kartı, banka kartı, havale)
- 🍽️ Mutfak kanban (Bekliyor → Hazırlanıyor → Servis)
- ⚙️ Masa ve menü yönetimi (ekle, düzenle, sil)
- 📊 Günlük gelir ve satış analizi
- 📱 Mobil uyumlu arayüz

---

## Mimari

```
Startup.bat
  └─ docker-compose up
       ├─ restaurantos-sqlserver   (SQL Server 2022, port 14330)
       └─ restaurantos-app         (Nginx + React + API, port 8080)
                │
                └─ sanal-network bağlantısı
                        │
                sanalsirket-nginx (port 80)
                  └─ restaurantos.local → restaurantos-app
```

---

## Geliştirme

```powershell
# Sadece DB başlat
docker-compose up -d sqlserver

# API
dotnet run --project RestaurantOS.Api

# React
cd web; npm install; npm run dev
```
