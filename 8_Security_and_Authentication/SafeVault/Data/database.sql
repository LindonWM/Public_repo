CREATE DATABASE IF NOT EXISTS SafeVault;
USE SafeVault;

CREATE TABLE IF NOT EXISTS Users (
	UserID INT PRIMARY KEY AUTO_INCREMENT,
	Username VARCHAR(100) NOT NULL,
	Email VARCHAR(100) NOT NULL,
	Role VARCHAR(50) NOT NULL DEFAULT 'User',
	PasswordHash VARCHAR(255) NOT NULL,
	UNIQUE KEY UX_Users_Username (Username),
	UNIQUE KEY UX_Users_Email (Email)
);

-- Passwords hashed with BCrypt work-factor 12.
-- If upgrading from plaintext storage run first:
--   ALTER TABLE Users CHANGE COLUMN Password PasswordHash VARCHAR(255) NOT NULL;
INSERT INTO Users (Username, Email, Role, PasswordHash)
VALUES
  ('demo_user',      'demo@example.com',              'User',  '$2a$12$4kabOMY3XzpGr/lDWRu.2.CAbQ7lFjb129T9OVnauVPCBQHyWPS6u'),
  ('anna_kowalska',  'anna.kowalska@example.com',     'Admin', '$2a$12$wlL2e/BwlfwFdTaoaO5DA.ieLBBi3DfVuhmB03vhjSc/LBmKiGl66'),
  ('mike_nowak',     'mike.nowak@example.com',        'User',  '$2a$12$d3u6F5uOvrwByq9X9BLQ4OBdoQ3paTtXv6XE/.qB70tFbm0UlEjua'),
  ('sara_wisniewska','sara.wisniewska@example.com',   'User',  '$2a$12$BwvWsEphCgL2lAFGwqfd2ujw3hHisRByfmU5RneJppV830S1QScKe')
ON DUPLICATE KEY UPDATE
	Email        = VALUES(Email),
	Role         = VALUES(Role),
	PasswordHash = VALUES(PasswordHash);