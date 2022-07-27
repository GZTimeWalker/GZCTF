import { FC } from 'react'
import { useNavigate } from 'react-router-dom'
import { Accordion, Avatar, Box, Button, Group, Paper, Text, useMantineTheme } from '@mantine/core'
import { mdiBackburger, mdiCheck, mdiClose, mdiCrown } from '@mdi/js'
import { Icon } from '@mdi/react'
import WithGameTab from '../../../../components/admin/WithGameTab'

const GameTeamReview: FC = () => {
  const navigate = useNavigate()
  const theme = useMantineTheme()
  return (
    <WithGameTab
      headProps={{ position: 'left' }}
      head={
        <Button
          leftIcon={<Icon path={mdiBackburger} size={1} />}
          onClick={() => navigate('/admin/games')}
        >
          返回上级
        </Button>
      }
    >
      <Paper shadow="md">
        <Accordion variant="contained" chevronPosition="left" defaultValue="team1">
          <Accordion.Item value="team1">
            <Box sx={{ display: 'flex', alignItems: 'center' }}>
              <Accordion.Control>
                <Group>
                  <Avatar />
                  <Box>
                    <Text>队伍名</Text>
                    <Text size="sm" color="dimmed" weight={400}>
                      队伍签名
                    </Text>
                  </Box>
                </Group>
              </Accordion.Control>
              <Group noWrap sx={{ padding: '16px' }}>
                <Button leftIcon={<Icon path={mdiCheck} size={1} />} loading>接受</Button>
                <Button leftIcon={<Icon path={mdiClose} size={1} />} color="red" disabled>拒绝</Button>
              </Group>
            </Box>
            <Accordion.Panel>
              <Group spacing="xl">
                <Group>
                  <Avatar />
                  <Box>
                    <Group>
                      <Box>Name</Box>
                      <Box>Real Name</Box>
                    </Group>
                    <Box>12345678</Box>
                  </Box>
                </Group>
                <Icon path={mdiCrown} size={1} color={theme.colors.yellow[4]} />
                <Box>user@example.com</Box>
                <Box>+86 136 1234 5678</Box>
              </Group>
            </Accordion.Panel>
          </Accordion.Item>

          <Accordion.Item value="customization">
            <Accordion.Control>Customization</Accordion.Control>
            <Accordion.Panel>Colors, fonts, shadows and many other parts are customizable to fit your design needs</Accordion.Panel>
          </Accordion.Item>

          <Accordion.Item value="flexibility">
            <Accordion.Control>Flexibility</Accordion.Control>
            <Accordion.Panel>Configure components appearance and behavior with vast amount of settings or overwrite any part of component styles</Accordion.Panel>
          </Accordion.Item>

          <Accordion.Item value="focus-ring">
            <Accordion.Control>No annoying focus ring</Accordion.Control>
            <Accordion.Panel>With new :focus-visible pseudo-class focus ring appears only when user navigates with keyboard</Accordion.Panel>
          </Accordion.Item>
        </Accordion>
      </Paper>
    </WithGameTab >
  )
}

export default GameTeamReview
