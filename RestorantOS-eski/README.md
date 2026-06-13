# RestaurantOS

Restoran yönetim sistemi: React + ASP.NET Core API + SQL Server (Docker).

---

## Kurulum (Yeni PC)

```bat
Startup.bat
```

Çift tıklayın. Herşeyi otomatik kurar:
- Docker Desktop (yoksa indirir ve kurar)
- WSL2 etkinleştirme (gerekirse)
- Veritabanı oluşturma
- Uygulama build + başlatma
- Güvenlik duvarı kuralları
- PC restart sonrası otomatik açılma

---

## Giriş Bilgileri

| Kullanıcı | Şifre              | Rol      |
| --------- | ------------------ | -------- |
| **admin** | `Resto@Admin2024!` | Yönetici |
| ahmet     | `Garson@2024!`     | Garson   |
| ayse      | `Kasiyer@2024!`    | Kasiyer  |

---

## Erişim

| Cihaz            | Adres                            |
| ---------------- | -------------------------------- |
| Bu bilgisayar    | http://localhost:8080             |
| Ağdaki diğer PC  | http://SUNUCU_IP:8080            |
| Telefon / Tablet | Aynı WiFi'ye bağlanıp IP yazın   |

> **Not:** Sunucu IP adresi `Startup.bat` çalıştıktan sonra ekranda gösterilir.

### Mobil Erişim

Telefonla giriş yapmak için: sunucu bilgisayarın IP adresini telefon tarayıcısına yazın (örn: `http://192.168.1.100:8080`). Veya giriş ekranındaki **QR kodu** telefonunuzla tarayın.

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
