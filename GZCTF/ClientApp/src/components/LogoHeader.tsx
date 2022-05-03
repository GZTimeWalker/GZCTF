import { FC } from 'react';
import { Group, Title, createStyles } from '@mantine/core';
import MainIcon from './icon/MainIcon';

const useStyles = createStyles((theme) => ({
  brand: {
    color: theme.colors[theme.primaryColor][4],
  },
  title: {
    marginLeft: '-20px',
    marginBottom: '-5px',
  },
}));

const LogoHeader: FC = () => {
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

export default LogoHeader;
