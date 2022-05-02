import { FC } from 'react';
import {
  Stack,
  Center,
  createStyles,
} from '@mantine/core';
import AccountHeader from './AccountHeader';

const useStyles = createStyles((theme) => ({
  input: {
    width: '20%',
    minWidth: '250px',
    maxWidth: '300px'
  },
}));

const AccountView: FC<React.ReactNode> = ({ children }) => {
  const { classes, cx } = useStyles();

  return (
    <Center style={{ height: '100vh' }}>
      <Stack align="center" justify="center" className={cx(classes.input)}>
        <AccountHeader />
        {children}
      </Stack>
    </Center>
  );
};

export default AccountView;
