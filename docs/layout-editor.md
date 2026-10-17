# Layout Editor (odaları elle düzenleme)

1. `SixtyOne > Layout Editor` aç. Soldan bir oda seç: oda tek başına gösterilir ve kamera ona odaklanır (Prev / Next ile gez).
2. Eşyayı Scene'de Move aracıyla (W) sürükle. "Izgaraya yapıştır" açıkken yarım hücreye oturur. Kırmızı hücreler yolu kapatır.
3. Yeni eşya: palet sekmeleri (Eşyalar / Toplanabilir / Dolaplar / Işık) → tıkla, oda ortasında belirir. Ctrl+D çoğaltır, Delete siler.
4. Seçili eşyanın ayarları (item, süre, ödünç, dolap içindekiler, flip, hazard) pencerenin altında.
5. 🔒 işaretliler (anchor, kapı, spawn, NPC, halat) kodda kalır, taşınmaz.
6. `Save room` (ya da `Save all`) → `Rebuild scene`. Değişiklikler her rebuild'de geri gelir.
7. `Validate Content` kapıyı / anchor'ı kapatan eşyaları oda + eşya id'siyle yazar.
8. Oyunda kontrol: Play'de F9 → oda listesi → odaya ışınlan (sadece Editor / Development build).
9. Kaydedilen her şey `Assets/_Game/Data/Layout/RoomLayoutOverrides.asset` içinde: bu dosyayı commit et.
