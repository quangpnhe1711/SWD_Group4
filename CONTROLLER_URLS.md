# Controller URLs (SWD_Group4.Presentation)

## Routing rule
Project dùng **conventional routing** trong `Program.cs`:

- Pattern: `/{controller}/{action}/{id?}`
- Default landing: `/Refund/ViewRefundRequests`
- Cookie auth redirect:
  - LoginPath: `/Auth/Login`
  - AccessDeniedPath: `/Auth/Login`

Ghi chú:
- Các action `[HttpPost]` đều có `[ValidateAntiForgeryToken]` ⇒ request cần gửi kèm CSRF token.
- Một số endpoint user-side (Verification) có thể đang bị ẩn khỏi navbar để mock dữ liệu, nhưng route vẫn tồn tại.

---

## AuthController
Base: `/Auth/{action}`

| Method | URL | Auth | Notes |
|---|---|---|---|
| GET | `/Auth/Register` | Public | Hiển thị form register |
| POST | `/Auth/registerUser` | Public | Submit register |
| GET | `/Auth/Login` | Public | Hiển thị form login |
| POST | `/Auth/loginUser` | Public | Submit login; success redirect `/Refund/ViewRefundRequests` |
| POST | `/Auth/Logout` | Auth | Sign out; redirect `/Auth/Login` |

---

## RefundController
Base: `/Refund/{action}`

| Method | URL | Auth | Notes |
|---|---|---|---|
| GET | `/Refund/ViewRefundRequests?sellerId={sellerId}` | Public | `sellerId` default = 1 |
| POST | `/Refund/ApproveRefund` | Public | form fields: `sellerId`, `refundId`, `decision` (default `confirm`, `cancel` to abort) |
| POST | `/Refund/RejectRefund` | Public | form fields: `sellerId`, `refundId`, `reason`, `decision` (default `confirm`, `cancel` to abort) |

---

## AdminVerificationController
Base: `/AdminVerification/{action}`

Authorization:
- **Demo mode**: không bắt buộc Admin/login ở controller này.

| Method | URL | Auth | Notes |
|---|---|---|---|
| GET | `/AdminVerification/viewPendingRequests` | Public | Pending list |
| GET | `/AdminVerification/getDetail?requestId={id}` | Public | Detail/compare view (flow: pending → get detail) |
| GET | `/AdminVerification/requestDetails?requestId={id}` | Public | Alias/backwards compatible với view cũ |
| POST | `/AdminVerification/approveRequest` | Public | form field: `requestId` |
| POST | `/AdminVerification/rejectRequest` | Public | form fields: `RequestId`, `Reason` (min 20 chars theo BR-OPS-04) |
| GET | `/AdminVerification/viewSellerList` | Public | List "Seller" users (Role=Seller or KYC fields filled) |
| POST | `/AdminVerification/suspendSeller` | Public | form fields: `UserId`, `Reason` (min 20 chars), `DurationType` (`OneWeek`/`OneMonth`/`OneYear`/`Permanent`) |
| POST | `/AdminVerification/unsuspendSeller` | Public | form field: `userId` |

---

## VerificationController
Base: `/Verification/{action}`

Authorization:
- Controller có `[Authorize]` ⇒ cần login.

| Method | URL | Auth | Notes |
|---|---|---|---|
| GET | `/Verification/Submit` | Auth | Form submit verification request |
| POST | `/Verification/submitVerificationRequest` | Auth | Submit verification request |
| GET | `/Verification/MyRequests` | Auth | List request của user |
