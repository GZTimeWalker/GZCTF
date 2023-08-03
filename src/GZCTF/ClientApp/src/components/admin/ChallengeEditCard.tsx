import { Dispatch, FC, SetStateAction, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Card,
  Group,
  Switch,
  Progress,
  Badge,
  ActionIcon,
  Text,
  useMantineTheme,
  Tooltip,
} from '@mantine/core'
import { mdiDatabaseEditOutline, mdiPuzzleEditOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { ChallengeTagLabelMap } from '@Utils/Shared'
import { ChallengeInfoModel, ChallengeTag } from '@Api'

interface ChallengeEditCardProps {
  challenge: ChallengeInfoModel
  onToggle: (challenge: ChallengeInfoModel, setDisabled: Dispatch<SetStateAction<boolean>>) => void
}

const ChallengeEditCard: FC<ChallengeEditCardProps> = ({ challenge, onToggle }) => {
  const data = ChallengeTagLabelMap.get(challenge.tag as ChallengeTag)
  const theme = useMantineTheme()
  const navigate = useNavigate()
  const { id } = useParams()

  const colors = theme.colors[data?.color ?? 'brand']
  const [disabled, setDisabled] = useState(false)

  const [min, cur, tot] = [
    challenge.minScore ?? 0,
    challenge.score ?? 500,
    challenge.originalScore ?? 500,
  ]
  const minRate = (min / tot) * 100
  const curRate = (cur / tot) * 100

  const tooltipStyle = {
    tooltip: {
      color: theme.colorScheme === 'dark' ? 'white' : 'black',
      backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[6] : 'white',
    },
  }

  return (
    <Card shadow="sm">
      <Group noWrap position="apart">
        <Switch
          disabled={disabled}
          checked={challenge.isEnabled}
          onChange={() => onToggle(challenge, setDisabled)}
        />
        <Icon path={data!.icon} color={theme.colors[data?.color ?? 'brand'][5]} size={1} />
        <Group noWrap position="apart" spacing="sm" w="calc(100% - 100px)">
          <Text lineClamp={1} fw={700} w="14rem">
            {challenge.title}
          </Text>

          <Progress
            size="xl"
            w="calc(100% - 25rem)"
            radius="xl"
            sections={[
              { value: minRate, color: colors[9], label: `${challenge.minScore}` },
              { value: curRate - minRate, color: colors[7], label: `${challenge.score}` },
            ]}
          />

          <Text size="xs" fw={700} w="2.5rem">
            {challenge.originalScore}pts
          </Text>
          <Group position="right" w="8rem">
            <Badge color={data?.color} variant="dot">
              {data?.label}
            </Badge>
          </Group>
        </Group>
        <Tooltip label="编辑题目信息" position="left" width={120} offset={10} styles={tooltipStyle}>
          <ActionIcon
            onClick={() => {
              navigate(`/admin/games/${id}/challenges/${challenge.id}`)
            }}
          >
            <Icon path={mdiPuzzleEditOutline} size={1} />
          </ActionIcon>
        </Tooltip>
        <Tooltip
          label="编辑附件及 flag"
          position="left"
          width={120}
          offset={54}
          styles={tooltipStyle}
        >
          <ActionIcon
            onClick={() => {
              navigate(`/admin/games/${id}/challenges/${challenge.id}/flags`)
            }}
          >
            <Icon path={mdiDatabaseEditOutline} size={1} />
          </ActionIcon>
        </Tooltip>
      </Group>
    </Card>
  )
}

export default ChallengeEditCard
