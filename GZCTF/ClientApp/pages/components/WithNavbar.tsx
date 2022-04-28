import React, { FC } from 'react';
import { AppNavbar } from './AppNavbar';
import { AppShell, Box, Center } from '@mantine/core';

export const WithNavBar: FC<any> = ({ children }) => {
  return (
    <AppShell
      padding="md"
      fixed
      navbar={<AppNavbar />}
      styles={(theme) => ({
        main: {
          backgroundColor:
            theme.colorScheme === 'dark' ? theme.colors.gray[7] : theme.white,
        },
      })}
    >
      <Center style={{ width: '100%' }}>
        <Box style={{ width: '80%' }}>{children}</Box>
      </Center>
    </AppShell>
  );
};
