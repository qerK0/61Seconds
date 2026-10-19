# Layout Editor (odaları elle düzenleme)

1. `SixtyOne > Layout Editor` aç. Soldan bir oda seç: oda tek başına gösterilir ve kamera ona odaklanır (Prev / Next ile gez).
2. Eşyayı Scene'de Move aracıyla (W) sürükle. "Izgaraya yapıştır" açıkken yarım hücreye oturur. Kırmızı hücreler yolu kapatır.
3. Yeni eşya: palet sekmeleri (Eşyalar / Toplanabilir / Dolaplar / Işık) → tıkla, oda ortasında belirir. Ctrl+D çoğaltır, Delete siler.
   Dolaplar: Gardırop, Gardırop v2, Dolap, Mutfak dolabı, Kitaplık, Çekmece, Alet çantası, Komodin.
4. Seçili eşyanın ayarları (item, süre, ödünç, dolap içindekiler, flip, hazard) pencerenin altında.
5. **Ölçek:** "Ölçek" kaydırıcısı (0.5–2) ya da Scene'deki Scale aracı (R); bırakınca hep eşit (uniform) olur.
   - `Reset scale` → 1.
   - `Match prop height` → prop'u PropArt tablosundaki hedef yüksekliğe getirir.
   - Yolu kapatan eşya büyüdükçe daha çok hücre kapatır (ölçek 2 = 2x2 hücre).
   - Eski kayıtlar ölçeksiz yüklenir (1).
6. **Sıralama (önde +):** eşya başka bir şeyin arkasında/önünde kalıyorsa –/+ ile biraz kaydır.
7. **Kapılar** (merdiven kapısı dahil): Move ile duvar boyunca sürükle; bırakınca en yakın duvar boşluğuna (L0.., R0..) oturur.
   Ya da "Duvar boşluğu" listesinden seç. Flip ve görsel (Door / Elevator / Stairwell) değişebilir.
   Nereye açıldığı, seçimi ve süresi kodda kalır (sadece gösterilir). Kapıyı taşıyınca durduğun nokta ve
   karşı odada çıktığın nokta da onunla gelir. Kapılar silinemez.
8. 🔒 işaretliler (anchor, spawn, NPC, halat) kodda kalır, taşınmaz.
9. `Save room` (ya da `Save all`) → `Rebuild scene`. Değişiklikler her rebuild'de geri gelir.
10. `Validate Content` kapıyı / anchor'ı kapatan eşyaları, aynı boşluğa düşen iki kapıyı ve kesilen yolları oda + id ile yazar.
11. Oyunda kontrol: Play'de F9 → oda listesi → odaya ışınlan (sadece Editor / Development build).
12. Kaydedilen her şey `Assets/_Game/Data/Layout/RoomLayoutOverrides.asset` içinde: bu dosyayı commit et.
