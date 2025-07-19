# Team7_SE1837_SWP391_BDSS
 Leader: Vo Hoang Thang
 
 Member: Le Vu Van, Nguyen Tuan Kiet, Quan Ba Thanh, Nguyen Viet Huy
 
 Topic: Blood Donation Support System

A - System Design:
1. Architecture:
- Presentation Layer: ASP.NET WebForms (user interaction).
- Business Logic Layer: Manages processing and workflows.
- Data Access Layer: Communicates with SQL Server.
2. Technology:
- Frontend: ASP.NET WebForms, HTML/CSS, Javascript  (Visual Studio 2022)
- Backend: C# (Visual Studio 2022)
- Database: SSMS, SQL Server (Microsoft SQL Server 2022)
- Web Server: IIS 10.0 (Internet Information Services)
- Communication: Direct query, use external API to support functional complex

B - Actors & Features
1. Actors:
- Guest: Only view public pages such as Homepage,News or Blogs and register to become a donor.
- Donor: A registered user who donates blood and can access their own donation data, certificates, and notifications.
- Staff: Medical or organizational personnel who manage blood requests, manage donation registrations, access inventory and find nearby donors. 
- Admin: The system administrator who manages users, system reports, dashboards, and blood request.
- Medical Center: Representatives from medical centers can view requests, submit blood requests, request emergency care, view and complete arrived donors donation status.
2. Features:
  a. Booking Donation Appointment:
  - Function trigger: Donor logs in then navigates to the “Book Donation Appointment” page.
  - Function description:
    + Actors: Donor
    + Purpose: Book a blood donation appointment.
    + Interface: Select date, blood type and time slot.
    + Data processing: Validate availability and eligibility. Save booking info.
  - Function Details:
    + Validation: Date must be in the future, slot must be available.
    + Business logic: A Donor can only book one appointment at a time.
    + Normal case: Appointment booked then confirmation slip shown.
    + Abnormal case: If there is a duplicate/conflicting slot, the system shows an error message.
  b. Normal Blood Request:
  - Function trigger: The Medical Center accesses the system and selects the option to "Create Blood Request". 
  - Function description: 
    + Actors: Medical Center. 
    + Purpose: Submit a blood request to seek available blood units for emergency or scheduled needs. 
    + Interface: Blood Request Form (Fields: Blood Type, Quantity, Request Reason, Compatibility). 
  - Data processing: 
    + Save the request with status Pending into the database. 
    + Notify Admin of a new request via the dashboard or email. 
  - Functional Details:
    + Validation:
      * User is authenticated and authorized.
      * Required fields are filled and valid.
    + Business Logic:
      * Save new blood request with status "Pending".
      * Normal Case
      * Request is created and visible in the list.
  - Abnormal Case:
    + Missing/invalid data: show error.
    + System error: show error message.
  c. Emergency Blood Request:
  - Business Logic:
    + Geolocation search for donors within X km of the Medical Center.
    + Filter by blood type and eligibility.
    + Notify all matching donors.
    + Track and display donor responses.
  - Normal Case:
    + System finds and notifies eligible nearby donors.
    + Medical Center sees list of notified donors and their responses.
  - Abnormal Case:
    + No eligible donors found: show alert to Medical Center.
    + Notification failure: show error message.
    + System/database error: show error message.



