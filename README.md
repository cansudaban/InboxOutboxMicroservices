# Inbox/Outbox Pattern Microservices

Bu proje, .NET Core ile geliştirilmiş Inbox/Outbox pattern kullanarak güvenilir mesaj iletimi sağlayan mikroservis mimarisinin örnek implementasyonudur.

## 🏗️ Mimari Genel Bakış

Proje dört ana mikroservisten oluşmaktadır:

- **Order Service** (Port: 5001) - Sipariş yönetimi
- **Stock Service** (Port: 5002) - Stok yönetimi  
- **Billing Service** (Port: 5003) - Faturalama işlemleri
- **API Gateway** (Port: 5000) - Tek giriş noktası

### 🔧 Kullanılan Teknolojiler

- **.NET 8.0** - Ana framework
- **ASP.NET Core Web API** - REST API geliştirme
- **RabbitMQ** - Message broker
- **Docker & Docker Compose** - Konteynerizasyon
- **Ocelot** - API Gateway
- **Entity Framework Core** - ORM

## 📋 Özellikler

### ✅ Inbox Pattern
- Gelen mesajların güvenli şekilde işlenmesi
- Duplicate mesajların engellenmesi
- İşlem güvenilirliği

### ✅ Outbox Pattern  
- Dış servislere gönderilecek mesajların güvenli saklanması
- Transaction güvenliği
- Mesaj gönderim garantisi

### ✅ Mikroservis Mimarisi
- Loosely coupled servisler
- Independent deployment
- Scalability

## 🚀 Kurulum ve Çalıştırma

### Ön Gereksinimler

- Docker Desktop
- .NET 8.0 SDK (geliştirme için)

### Docker ile Çalıştırma

1. Projeyi klonlayın:
```bash
git clone https://github.com/cansudaban/InboxOutboxMicroservices.git
cd InboxOutboxMicroservices
```

2. Tüm servisleri ayağa kaldırın:
```bash
docker-compose up -d
```

3. Servislerin durumunu kontrol edin:
```bash
docker-compose ps
```

### Manuel Çalıştırma (Geliştirme)

1. RabbitMQ'yu başlatın:
```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

2. Her servisi ayrı terminalde çalıştırın:
```bash
# Order Service
cd OrderService
dotnet run

# Stock Service  
cd StockService
dotnet run

# Billing Service
cd BillingService
dotnet run

# Gateway
cd Gateway
dotnet run
```

## 🌐 API Endpoints

### API Gateway (http://localhost:5000)
Ana giriş noktası - tüm istekler buradan yönlendirilir

### Order Service (http://localhost:5001)
- `GET /api/orders` - Tüm siparişleri listele
- `GET /api/orders/{id}` - Belirli sipariş detayı
- `POST /api/orders` - Yeni sipariş oluştur

### Stock Service (http://localhost:5002)
- `GET /api/stock` - Stok durumunu görüntüle
- `PUT /api/stock/reserve` - Stok rezervasyonu

### Billing Service (http://localhost:5003)
- `GET /api/invoices` - Fatura listesi
- `POST /api/invoices` - Yeni fatura oluştur

## 📊 Monitoring

### RabbitMQ Management
- URL: http://localhost:15672
- Username: `guest`
- Password: `guest`

## 🔄 Mesaj Akışı

1. **Sipariş Oluşturma**: Order Service'e POST isteği
2. **Event Publishing**: OrderCreated eventi Outbox'a kaydedilir
3. **Message Dispatch**: Background service eventi RabbitMQ'ya gönderir
4. **Event Consumption**: Stock ve Billing servisleri eventi alır
5. **Inbox Processing**: Her servis gelen mesajı Inbox'ında işler
6. **Business Logic**: İlgili iş mantığı çalıştırılır

## 📁 Proje Yapısı

```
├── OrderService/           # Sipariş yönetim servisi
│   ├── Controllers/        
│   ├── Models/            
│   ├── Services/          
│   ├── BackgroundServices/ # Outbox dispatcher
│   └── Data/              
├── StockService/           # Stok yönetim servisi
├── BillingService/         # Faturalama servisi  
├── Gateway/                # API Gateway (Ocelot)
├── Contracts/              # Shared contracts ve events
└── docker-compose.yml      # Container orchestration
```

## 🧪 Test Etme

### Örnek Sipariş Oluşturma

```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "John Doe",
    "items": [
      {
        "productId": "123e4567-e89b-12d3-a456-426614174000",
        "productName": "Sample Product",
        "quantity": 2,
        "unitPrice": 29.99
      }
    ]
  }'
```

## 🔍 Troubleshooting

### Yaygın Sorunlar

1. **Port çakışması**: `docker-compose down` ile servisleri durdurun
2. **RabbitMQ bağlantı hatası**: RabbitMQ container'ının ayakta olduğunu kontrol edin
3. **Servis başlamıyor**: `docker-compose logs <service-name>` ile logları kontrol edin

## 🚧 Geliştirme Notları

- Her servis kendi veritabanına sahiptir (Database per Service pattern)
- Mesaj garantisi için Outbox/Inbox pattern implementasyonu
- Idempotent message processing
- Distributed transaction yönetimi

## 📝 Lisans

Bu proje MIT lisansı altında lisanslanmıştır.

## 🤝 Katkıda Bulunma

1. Fork edin
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Değişikliklerinizi commit edin (`git commit -m 'Add amazing feature'`)
4. Branch'inizi push edin (`git push origin feature/amazing-feature`)
5. Pull Request oluşturun

---

⭐ Bu projeyi beğendiyseniz yıldız vermeyi unutmayın!
