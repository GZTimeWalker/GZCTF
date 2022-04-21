import React, { FC } from 'react';
import { AppNavbar } from './AppNavbar';
import { AppShell } from '@mantine/core';

export const WithNavBar: FC = ({ children }) => {
    return (
      <AppShell
        padding="md"
        fixed
        navbar={<AppNavbar />}
        styles={(theme) => ({
          main: {
            backgroundColor: theme.colorScheme === 'dark' ? theme.colors.gray[7] : theme.colors.gray[0],
          }
        })}
      >
        {children}
      </AppShell>
    );
  };
