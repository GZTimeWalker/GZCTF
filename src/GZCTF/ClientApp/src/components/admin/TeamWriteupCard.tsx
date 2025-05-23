import {
  ActionIcon,
  Avatar,
  Card,
  Group,
  CardProps,
  Stack,
  Text,
  useMantineColorScheme,
  useMantineTheme,
} from '@mantine/core'
import { mdiDownload } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC } from 'react'
import { Link } from 'react-router'
import { useLanguage } from '@Utils/I18n'
import { WriteupInfoModel } from '@Api'
import classes from '@Styles/HoverCard.module.css'

interface TeamWriteupCardProps extends CardProps {
  writeup: WriteupInfoModel
  selected?: boolean
  onClick: () => void
}

export const TeamWriteupCard: FC<TeamWriteupCardProps> = ({ writeup, selected, ...props }) => {
  const { colorScheme } = useMantineColorScheme()
  const { locale } = useLanguage()
  const theme = useMantineTheme()
  return (
    <Card
      {...props}
      p="sm"
      shadow="sm"
      classNames={classes}
      style={{
        border: `2px solid ${
          selected ? theme.colors[theme.primaryColor][colorScheme === 'dark' ? 8 : 6] : 'transparent'
        }`,
      }}
    >
      <Group wrap="nowrap" gap={3} justify="space-between">
        <Group gap="sm" wrap="nowrap" justify="space-between" maw="calc(100% - 2rem)">
          <Avatar alt="avatar" src={writeup.team?.avatar} size="md">
            {writeup.team?.name?.slice(0, 1)}
          </Avatar>
          <Stack gap={0} justify="space-between" maw="calc(100% - 3rem)">
            <Text size="0.8rem" lineClamp={1} c="dimmed">
              #{writeup.team?.id}
            </Text>
            <Text lineClamp={1} fw={600}>
              {writeup.team?.name}
            </Text>
            <Text size="xs" lineClamp={1} c="dimmed">
              {dayjs(writeup.uploadTimeUtc).locale(locale).format('SLL LT')}
            </Text>
          </Stack>
        </Group>
        <ActionIcon component={Link} target="_blank" to={writeup.url ?? '#'}>
          <Icon path={mdiDownload} size={1} />
        </ActionIcon>
      </Group>
    </Card>
  )
}
