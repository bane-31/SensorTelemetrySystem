# Primenjene bezbednosne mere

## 1. Zaštita od Replay napada
Svaka poruka koja pristiže u sistem (`MetricSubmissionDTO`) sadrži `MessageId`. Server u bazi čuva poslednji primljeni ID za svaki senzor. Ukoliko stigne poruka sa manjim ili jednakim ID-jem, sistem je odbija, čime se sprečava ponovno slanje presretnutih (validnih) poruka.

## 2. Zaštita od DoS napada (Rate Limiting)
Implementiran je `Fixed Window` limiter koji ograničava broj zahteva po senzoru na 10 poruka u sekundi. Prekoračenje limita rezultuje HTTP 429 (Too Many Requests) odgovorom, čime se čuva stabilnost Ingestion servisa.

## 3. Resilience & Self-Healing (Fault Tolerance)
* **Detekcija otkaza:** Sistem prati `LastSeen` timestamp za svaki senzor. Senzori koji se nisu oglasili duže od 10 sekundi smatraju se neaktivnim, što pokreće mehanizam zamene.

## 4. Pouzdana komunikacija (Kriptografija - U izradi)


## 5. Izolacija na mrežnom nivou
* **Ingress:** U Kubernetes okruženju, pristup sistemu je ograničen isključivo preko Ingress kontrolera koji deluje kao jedina ulazna tačka, skrivajući direktan pristup mikroservisima.