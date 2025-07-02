## Business Rules

| ID   | Business Rule               | Business Rule Description                                                                 |
|------|-----------------------------|--------------------------------------------------------------------------------------------|
| BR-01 | User Role Permissions       | Only Admin or Librarian can create, update, and delete user accounts.                     |
| BR-02 | User Roles                  | Each user must have one role: Member, Librarian, or Admin.                                |
| BR-03 | User Information Access     | Users can only view their own information; staff access is role-based.                    |
| BR-04 | User Data Privacy           | Sensitive user data must be protected from unauthorized access.                           |
| BR-05 | Password Security           | Passwords must be stored securely and meet minimum complexity requirements.               |
| BR-06 | Book Management Rights      | Only Librarian or Admin can add, edit, or delete books in the system.                     |
| BR-07 | Book Deletion Restriction   | Books with active loans or reservations cannot be deleted.                                |
| BR-08 | Copy Deletion Restriction   | Book copies with active loans or reservations cannot be deleted.                          |
| BR-09 | Copy Status Rules           | Copy statuses include: Available, On Loan, Reserved, Lost.                                |
| BR-10 | Copy Return Validation      | A copy cannot be marked as Available unless it has been properly returned.                |
| BR-11 | Category Management         | Only Librarian or Admin can create, edit, or delete categories.                           |
| BR-12 | Category Deletion Rules     | Categories with assigned books cannot be deleted.                                         |
| BR-13 | Loan Limit                  | Members can borrow up to 5 books at the same time.                                        |
| BR-14 | Standard Loan Period        | The default loan period for each book is 14 days.                                         |
| BR-15 | Overdue Fine Calculation    | Late returns incur fines calculated per overdue day.                                      |
| BR-16 | Loan Eligibility            | Members with unpaid fines or maxed-out loans cannot borrow more books.                    |
| BR-17 | Reservation Condition       | Reservations are allowed only if no copies are currently available.                       |
| BR-18 | Reservation Limit           | Each member can have up to 3 active reservations.                                         |
| BR-19 | Reservation Notification    | The system notifies the first member in the queue when a copy becomes available.          |
| BR-20 | Notification Requirement    | System sends notifications for overdue books, reservations, and fines.                    |
| BR-21 | Notification Format         | Notifications must include a subject and a clear message.                                 |
| BR-22 | Audit Logging Requirement   | Key actions (e.g., user creation, book loans, returns) must be logged with timestamps.    |
| BR-23 | Member Deletion Restriction | Members cannot be deleted if they have active loans, reservations, or unpaid fines.       |
| BR-24 | Role-Based Access Control   | System functionalities are restricted based on user roles (RBAC).                         |
| BR-25 | Staff Override Restrictions | Staff cannot cancel member reservations without proper authorization.                     |
| BR-26 | Password Reset Rate Limiting | Maximum 3 reset requests per email per hour |
