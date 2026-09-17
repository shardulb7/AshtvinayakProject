# Project Update — Summary for Client & Team

This document explains, in plain language, what work has been done on the Ashtavinayak Travel App backend and why it matters. Technical details are kept to a minimum — the goal is for anyone to understand what changed and what it means for the business.

---

## 1. Got the app working again

When this phase of work started, the project **did not compile at all** — a batch of admin panel screens had been corrupted by a faulty automated edit, so the whole application failed to build. This was the first thing fixed. The app now builds cleanly and runs.

Along the way, several smaller everyday bugs were also fixed — things like dropdown menus on edit screens showing the wrong list of options, a vehicle form that let invalid data through, and payment records that weren't updating correctly when a transaction's status changed.

## 2. Closed security gaps

A number of admin and API pages could previously be accessed without logging in, or let one logged-in user see data belonging to another user (for example, someone's private notifications or payment history). These have all been fixed — every page and API endpoint now correctly checks who is asking before showing or changing data.

We also found that login passwords and some sensitive credentials had at one point been stored in a way that wasn't secure, and that OTP (one-time password) codes were generated in a predictable way and were showing up in application logs. All of this has been corrected: passwords are properly encrypted, OTPs are generated securely, and no sensitive information is written to logs anymore.

**Important — needs your input:** a few real secret keys (payment gateway key, database password, SMS gateway password) were found saved directly in the project's history at some point before this work began. These should be treated as compromised and replaced with new ones before the app goes live publicly. This is flagged again in the deployment questions below.

## 3. Fixed a pricing security issue

Previously, the price of a booking was being taken directly from whatever the customer's app sent to the server — meaning a modified request could book a trip for a lower price than it should cost, or even a free/negative price. The server now always calculates the correct price itself, based on the actual package rates, and ignores any price the client tries to send. This closes a real financial risk.

## 4. Fixed a double-booking risk

Under heavy or simultaneous use, it was technically possible for the same seat on the same trip to be booked twice at the same moment (a "race condition"). A safeguard has been added at the database level so this can no longer happen, even under high load.

## 5. Completed a missing admin screen

The "Packages" section of the admin panel existed in the backend but had no working screens — trying to open it caused an error. This has been built out fully: list, view, add, edit, and delete packages, matching the look and feel of the rest of the admin panel.

## 6. New feature: Agent Registration & Booking

Built the full **Travel Agent module** as requested:
- Agents can register themselves and upload the required documents (ID proof, address proof, photo)
- An admin reviews and approves (or rejects) each agent before they can log in
- Approved agents can log in and book trips on behalf of customers
- Each booking made by an agent automatically calculates their commission — this can be a global default rate or a custom rate for a specific agent, both adjustable by the admin without any code changes
- A new admin report shows all agent bookings with filters by date, agent, status, and payment
- Commission rates are locked in at the time of booking, so changing the rate later never affects past bookings

## 7. Made the app production-ready

A set of improvements to make sure the app can be deployed and monitored reliably:
- Added "health check" pages the hosting platform can use to automatically confirm the app is running correctly
- Added protection so the app automatically retries if there's a brief network hiccup talking to the database, instead of failing outright
- Removed error messages that were exposing internal technical details to anyone calling the API — errors are now logged privately for the team, while users just see a friendly message
- Added a set of automated tests covering the most business-critical logic (pricing, commission calculation, document upload rules), so future changes can be checked automatically before release
- Added the packaging needed to run the app in a standard container (Docker), making it deployable on most modern hosting platforms
- Removed an outdated internal tool dependency that was flagged as a security risk and had no effect on the running app

---

## What's not done yet / needs a decision

- **Excel export** in a few admin screens uses an older internal library flagged with minor security advisories. It still works correctly; replacing it is a larger task that hasn't been scheduled yet.
- **Document storage** for agent uploads currently saves files to local disk. This works for now, but most cloud hosting platforms clear local disk on restart — before going live, we should decide whether to move this to permanent cloud storage.
- **Payment confirmation** currently relies on the customer's app telling the server "payment succeeded." For extra safety before handling real payments at scale, this should be backed by a direct server-to-server confirmation from the payment gateway.
- The compromised secret keys mentioned in section 2 need to be rotated (replaced with new ones) before public launch.

These are covered in more detail in the separate deployment-readiness questions shared with the client.
