
    document.addEventListener('DOMContentLoaded', function () {
        const form = document.getElementById('userForm');
    const submitButton = document.getElementById('submitButton');
    const cancelButton = document.getElementById('cancelButton');
    const userNameField = document.getElementById('UserName');
    const emailField = document.getElementById('Email');
    const phoneNumberField = document.getElementById('PhoneNumber');
    const passwordField = document.getElementById('PasswordHash');
    const roleField = document.getElementById('Role');

    // Function to validate email format
    function validateEmail(email) {
            const emailRegex = /^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9-]+(?:\.[a-zA-Z0-9-]+)*$/;
    return emailRegex.test(email);
        }

    // Function to validate phone number (10 digits)
    function validatePhoneNumber(phoneNumber) {
            const phoneRegex = /^\d{10}$/;
    return phoneRegex.test(phoneNumber);
        }

    // Function to validate password strength
    function validatePassword(password) {
            // Password must be at least 8 characters long and contain at least one number, one uppercase letter, and one special character
            const passwordRegex = /^(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/;
    return passwordRegex.test(password);
        }

    // Function to handle form submission
    function handleSubmit(event) {
        let formIsValid = true;

    // Clear previous error messages
    const errorMessages = document.querySelectorAll('.error-message');
    errorMessages.forEach(function (message) {
        message.remove();
            });

    if (!userNameField.value) {
        showError(userNameField, 'Username is required.');
    formIsValid = false;
            }

    const email = emailField.value;
    if (!email) {
        showError(emailField, 'Email is required.');
    formIsValid = false;
            } else if (!validateEmail(email)) {
        showError(emailField, 'Please enter a valid email address.');
    formIsValid = false;
            }

    const phoneNumber = phoneNumberField.value;
    if (!phoneNumber) {
        showError(phoneNumberField, 'Phone number is required.');
    formIsValid = false;
            } else if (!validatePhoneNumber(phoneNumber)) {
        showError(phoneNumberField, 'Phone number must be 10 digits.');
    formIsValid = false;
            }

    const password = passwordField.value;
    if (!password) {
        showError(passwordField, 'Password is required.');
    formIsValid = false;
            } else if (!validatePassword(password)) {
        showError(passwordField, 'Password must be at least 8 characters long and include at least one number, one uppercase letter, and one special character.');
    formIsValid = false;
            }

    const role = roleField.value;
    if (!role) {
        showError(roleField, 'Role is required.');
    formIsValid = false;
            }

    // If form is valid, show SweetAlert success message
    if (formIsValid) {
        // SweetAlert success message
        Swal.fire({
            title: 'Success!',
            text: 'Form submitted successfully.',
            icon: 'success',
            confirmButtonText: 'OK'
        }).then((result) => {
            if (result.isConfirmed) {
                form.submit(); // Submit form after SweetAlert confirmation
            }
        });
            } else {
        event.preventDefault();
            }
        }

    // Function to show error below the field
    function showError(field, message) {
            const errorMessage = document.createElement('div');
    errorMessage.classList.add('text-danger', 'error-message');
    errorMessage.textContent = message;
    field.parentElement.appendChild(errorMessage);
        }

    // Add event listener to the submit button
    submitButton.addEventListener('click', handleSubmit);

    // Cancel button functionality to reset the form
    cancelButton.addEventListener('click', function () {
        form.reset(); // Reset the form fields
    const errorMessages = document.querySelectorAll('.error-message');
    errorMessages.forEach(function (message) {
        message.remove(); // Remove any error messages if form is reset
            });
        });
    });
