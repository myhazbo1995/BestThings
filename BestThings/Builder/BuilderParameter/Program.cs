﻿using System;

namespace BuilderParameter
{
    public class MailService
    {
        public class EmailBuilder
        {
            public class Email
            {
                public string From, To, Subject, Body;
            }

            private readonly Email email;

            public EmailBuilder(Email email) => this.email = email;

            public EmailBuilder From(string from)
            {
                email.From = from;
                return this;
            }

            public EmailBuilder To(string to)
            {
                email.To = to;
                return this;
            }

            public EmailBuilder Subject(string subject)
            {
                email.Subject = subject;
                return this;
            }

            public EmailBuilder Body(string body)
            {
                email.Body = body;
                return this;
            }
        }

        private void SendEmailInternal(EmailBuilder.Email email) { }

        public void SendEmail(Action<EmailBuilder> builder)
        {
            var email = new EmailBuilder.Email();
            builder(new EmailBuilder(email));
            SendEmailInternal(email);
        }
    }

    class Program
    {
        public static void Main(string[] args)
        {
            var ms = new MailService();
            ms.SendEmail(email => email.From("foo@bar.com")
                .To("bar@baz.com")
                .Body("Hello, how are you?"));
        }
    }
}
