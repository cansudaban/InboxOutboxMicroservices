# Inbox/Outbox Pattern Microservices - Job Board System

Bu proje, .NET Core ile geliştirilmiş Inbox/Outbox pattern kullanarak güvenilir mesaj iletimi sağlayan mikroservis mimarisinin örnek implementasyonudur. Proje, bir **kariyer/iş ilanı platformu (Job Board)** senaryosu üzerinden tasarlanmıştır.

## 🎯 İş Senaryosu

Sistem, bir kariyer sitesinde iş ilanlarının yayınlanması ve başvuruların yönetilmesi süreçlerini içerir:

1. **İş İlanı Yayınlama**: Şirketler iş ilanı oluşturur (Job Service)
2. **Başvuru Takibi**: Adaylar başvuru yapar ve başvurular takip edilir (Application Service)
3. **Bildirim Gönderimi**: İş ilanları yayınlandığında ilgili adaylara bildirim gönderilir (Notification Service)

## 🏗️ Mimari Genel Bakış

Proje dört ana mikroservisten oluşmaktadır:

- **Job Service** (Port: 5001) - İş ilanları yönetimi
- **Application Service** (Port: 5002) - Başvuru yönetimi ve takibi
- **Notification Service** (Port: 5003) - Bildirim yönetimi
- **API Gateway** (Port: 5000) - Tek giriş noktası

### 🔧 Kullanılan Teknolojiler

- **.NET 9.0** - Ana framework
- **ASP.NET Core Web API** - REST API geliştirme
- **RabbitMQ** - Message broker
- **Docker & Docker Compose** - Konteynerizasyon
- **Ocelot** - API Gateway
- **Entity Framework Core** - ORM (InMemory Database)
- **Polly** - Retry ve resilience politikaları

## 📋 Özellikler

### ✅ Inbox Pattern
- Gelen mesajların güvenli şekilde işlenmesi (idempotency)
- Duplicate mesajların engellenmesi
- İşlem güvenilirliği ve tutarlılığı
- Dead Letter Queue ile hata yönetimi

### ✅ Outbox Pattern  
- Dış servislere gönderilecek mesajların güvenli saklanması
- Transaction güvenliği (Atomicity)
- Mesaj gönderim garantisi
- Background service ile otomatik mesaj iletimi

### ✅ Mikroservis Mimarisi
- Loosely coupled servisler
- Independent deployment
- Event-driven communication
- Scalability ve resilience

## 🚀 Kurulum ve Çalıştırma

### Ön Gereksinimler

- Docker Desktop
- .NET 9.0 SDK (geliştirme için)

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
# Job Service
cd JobService
dotnet run

# Application Service  
cd ApplicationService
dotnet run

# Notification Service
cd NotificationService
dotnet run

# Gateway
cd Gateway
dotnet run
```

## 🌐 API Endpoints

### API Gateway (http://localhost:5000)
Ana giriş noktası - tüm istekler buradan yönlendirilir

### Job Service (http://localhost:5001)
- `GET /api/jobs` - Tüm iş ilanlarını listele
- `GET /api/jobs/{id}` - Belirli iş ilanı detayı
- `POST /api/jobs` - Yeni iş ilanı oluştur

**Örnek POST Request:**
```json
{
  "companyName": "TechCorp Inc.",
  "jobTitle": "Senior Software Engineer",
  "jobDescription": "We are looking for an experienced software engineer...",
  "location": "Istanbul, Turkey",
  "employmentType": "Full-time",
  "salaryMin": 80000,
  "salaryMax": 120000,
  "requiredSkills": ["C#", ".NET", "Microservices", "RabbitMQ"],
  "applicationDeadline": "2025-12-31T23:59:59Z"
}
```

### Application Service (http://localhost:5002)
- `GET /api/applications` - Tüm başvuruları listele
- `GET /api/applications/{id}` - Belirli başvuru detayı
- `GET /api/applications/job/{jobId}` - Belirli iş ilanına yapılan başvurular
- `POST /api/applications` - Yeni başvuru oluştur

### Notification Service (http://localhost:5003)
- `GET /api/notifications` - Bildirim listesi
- `GET /api/notifications/{id}` - Belirli bildirim detayı
- `GET /api/notifications/job/{jobId}` - Belirli iş ilanı için gönderilen bildirimler
- `PATCH /api/notifications/{id}/resend` - Bildirimi yeniden gönder

## 📊 Monitoring

### RabbitMQ Management
- URL: http://localhost:15672
- Username: `guest`
- Password: `guest`

## 🔄 Mesaj Akışı

1. **İş İlanı Yayınlama**: Job Service'e POST isteği ile yeni iş ilanı oluşturulur
2. **Outbox Pattern**: JobPosted eventi transaction içinde Outbox tablosuna kaydedilir
3. **Message Dispatch**: Background service (OutboxDispatcher) Outbox'tan mesajları okur ve RabbitMQ'ya gönderir
4. **Event Broadcasting**: RabbitMQ fanout exchange üzerinden tüm subscriber servislere mesaj iletilir
5. **Inbox Pattern**: Application ve Notification servisleri eventi alır ve Inbox'a kaydeder
6. **Idempotency Check**: Mesajın daha önce işlenip işlenmediği kontrol edilir (duplicate prevention)
7. **Business Logic**: 
   - **Application Service**: İş ilanı bilgilerini loglar ve takibe alır
   - **Notification Service**: İlgili adaylara bildirim oluşturur ve kaydeder

## 📁 Proje Yapısı

```
├── JobService/             # İş ilanları yönetim servisi
│   ├── Controllers/        # JobsController
│   ├── Models/            # Job entity
│   ├── Services/          # JobService, MessageBusService
│   ├── BackgroundServices/ # OutboxDispatcher
│   └── Data/              # JobDbContext, OutboxMessage
├── ApplicationService/     # Başvuru yönetim servisi
│   ├── Controllers/        # ApplicationsController
│   ├── Models/            # JobApplication entity
│   ├── Services/          # MessageConsumerService (Inbox)
│   └── Data/              # ApplicationDbContext, InboxMessage
├── NotificationService/    # Bildirim servisi
│   ├── Controllers/        # NotificationsController
│   ├── Models/            # Notification entity
│   ├── Services/          # MessageConsumerService (Inbox)
│   └── Data/              # NotificationDbContext, InboxMessage
├── Gateway/                # API Gateway (Ocelot)
├── Contracts/              # Shared contracts ve events (JobPostedEventDto)
└── docker-compose.yml      # Container orchestration
```

## 🧪 Test Etme

### 1. İş İlanı Oluşturma

```bash
curl -X POST http://localhost:5001/api/jobs \
  -H "Content-Type: application/json" \
  -d '{
    "companyName": "TechCorp Inc.",
    "jobTitle": "Senior Software Engineer",
    "jobDescription": "We are looking for an experienced software engineer to join our team.",
    "location": "Istanbul, Turkey",
    "employmentType": "Full-time",
    "salaryMin": 80000,
    "salaryMax": 120000,
    "requiredSkills": ["C#", ".NET", "Microservices", "RabbitMQ", "Docker"],
    "applicationDeadline": "2025-12-31T23:59:59Z"
  }'
```

### 2. İş İlanlarını Görüntüleme

```bash
curl -X GET http://localhost:5001/api/jobs
```

### 3. Başvuru Oluşturma

```bash
curl -X POST http://localhost:5002/api/applications \
  -H "Content-Type: application/json" \
  -d '{
    "jobId": "{job-id-buraya}",
    "applicantName": "Jane Doe",
    "applicantEmail": "jane.doe@example.com",
    "applicantPhone": "+90 555 123 4567",
    "resume": "https://example.com/resumes/jane-doe.pdf",
    "coverLetter": "I am very interested in this position...",
    "yearsOfExperience": 5,
    "skills": ["C#", ".NET", "Microservices", "Azure", "Docker"]
  }'
```

### 4. Bildirimleri Görüntüleme

```bash
curl -X GET http://localhost:5003/api/notifications
```

### Gateway Üzerinden Test

```bash
# Gateway üzerinden iş ilanı oluşturma
curl -X POST http://localhost:5000/api/jobs \
  -H "Content-Type: application/json" \
  -d '{...}'
```

## 🔍 Troubleshooting

### Yaygın Sorunlar

1. **Port çakışması**: `docker-compose down` ile servisleri durdurun
2. **RabbitMQ bağlantı hatası**: RabbitMQ container'ının ayakta olduğunu kontrol edin
3. **Servis başlamıyor**: `docker-compose logs <service-name>` ile logları kontrol edin

## 🚧 Geliştirme Notları

### Pattern İmplementasyonları

**Outbox Pattern (Job Service)**
- İş ilanı oluşturma ve event publishing tek transaction içinde
- OutboxDispatcher background service ile otomatik mesaj gönderimi
- Retry mekanizması (Polly) ile güvenilirlik
- Message persistence

**Inbox Pattern (Application & Notification Services)**
- Gelen mesajların idempotent işlenmesi
- MessageId bazlı duplicate prevention
- Transaction içinde inbox kaydı ve business logic
- Dead Letter Queue ile hata yönetimi

### Mimari Kararlar

- **Database per Service**: Her servis kendi veritabanına sahip (InMemory)
- **Event-Driven Architecture**: RabbitMQ ile asenkron iletişim
- **API Gateway**: Ocelot ile routing ve load balancing
- **Resilience**: Polly ile retry ve circuit breaker
- **Health Checks**: Her serviste health endpoint
- **Containerization**: Docker ile kolay deployment

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
