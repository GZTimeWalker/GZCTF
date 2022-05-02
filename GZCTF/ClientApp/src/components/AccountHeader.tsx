import {
  Group,
  Title,
  createStyles,
} from '@mantine/core';
import MainIcon from './icon/MainIcon';
import { FC } from 'react';

const useStyles = createStyles((theme) => ({
  brand: {
    color: theme.colors[theme.primaryColor][4],
  },
  title: {
    marginLeft: '-20px',
    marginBottom: '-5px',
  },
}));

const AccountHeader: FC = () => {
  const { classes, cx } = useStyles();

  return (
    <Group>
      <MainIcon style={{ maxWidth: 60, height: 'auto' }} />
      <Title className={cx(classes.title)}>
        GZ<span className={cx(classes.brand)}>::</span>CTF
      </Title>
    </Group>
  );
};

export default AccountHeader;
