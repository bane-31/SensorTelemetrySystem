# Primenjene bezbednosne mere

## 1. Zaštita od Replay napada
Svaka poruka koja pristiže u sistem (`MetricSubmissionDTO`) sadrži `MessageId`. Server u bazi čuva poslednji primljeni ID za svaki senzor. Ukoliko stigne poruka sa manjim ili jednakim ID-jem, sistem je odbija, čime se sprečava ponovno slanje presretnutih (validnih) poruka.

## 2. Zaštita od DoS napada (Rate Limiting)
Implementiran je `Fixed Window` limiter koji ograničava broj zahteva po senzoru na 10 poruka u sekundi. Prekoračenje limita rezultuje HTTP 429 (Too Many Requests) odgovorom, čime se čuva stabilnost Ingestion servisa.

## 3. Resilience & Self-Healing (Fault Tolerance)
* **Detekcija otkaza:** Sistem prati `LastSeen` timestamp za svaki senzor. Senzori koji se nisu oglasili duže od 10 sekundi smatraju se neaktivnim, što pokreće mehanizam zamene.

## 4. Pouzdana komunikacija (Kriptografija - U izradi)
Komunikacija između SensorClient aplikacije i IngestionService servisa zaštićena je hibridnom kriptografijom.

Senzor ne šalje direktno običan DTO, već se podaci pakuju u SecureEnvelopeDTO. Sadržaj poruke se enkriptuje pomoću AES-GCM algoritma, dok se AES ključ enkriptuje javnim RSA ključem servera.

Pored enkripcije, senzor digitalno potpisuje payload svojim privatnim RSA ključem. Server nakon dekripcije proverava potpis pomoću javnog ključa senzora koji je sačuvan prilikom registracije.

Na ovaj način se obezbeđuje:

poverljivost poruke, jer sadržaj nije čitljiv bez privatnog ključa servera;
integritet poruke, jer se izmene payload-a detektuju;
autentičnost senzora, jer server proverava digitalni potpis senzora.

Prilikom secure registracije senzor šalje svoj javni ključ serveru, dok privatni ključ ostaje samo kod senzora. Server koristi svoj privatni ključ za dekripciju AES ključa, a javni ključ senzora za proveru potpisa.


## 5. Izolacija na mrežnom nivou
* **Ingress:** U Kubernetes okruženju, pristup sistemu je ograničen isključivo preko Ingress kontrolera koji deluje kao jedina ulazna tačka, skrivajući direktan pristup mikroservisima.