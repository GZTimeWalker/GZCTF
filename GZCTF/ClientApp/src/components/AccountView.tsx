import { FC } from 'react'
import { Stack, Center, createStyles } from '@mantine/core'
import LogoHeader from './LogoHeader'

const useStyles = createStyles(() => ({
  input: {
    width: '20%',
    minWidth: '250px',
    maxWidth: '300px',
  },
}))

const AccountView: FC<React.PropsWithChildren> = ({ children }) => {
  const { classes, cx } = useStyles()

  return (
    <Center style={{ height: '100vh' }}>
      <Stack align="center" justify="center" className={cx(classes.input)}>
        <LogoHeader />
        {children}
      </Stack>
    </Center>
  )
}

export default AccountView
