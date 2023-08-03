import { FC } from 'react'
import { Group, GroupProps, MantineNumberSize, useMantineTheme } from '@mantine/core'
import { ActionIconWithConfirm } from '@Components/ActionIconWithConfirm'
import { ParticipationStatusMap } from '@Utils/Shared'
import { ParticipationStatus } from '@Api'

interface ParticipationStatusControlProps extends GroupProps {
  disabled: boolean
  participateId: number
  size?: MantineNumberSize
  status: ParticipationStatus
  setParticipationStatus: (id: number, status: ParticipationStatus) => Promise<void>
}

export const ParticipationStatusControl: FC<ParticipationStatusControlProps> = (props) => {
  const { disabled, participateId, status, setParticipationStatus, size, ...others } = props
  const part = ParticipationStatusMap.get(status)!
  const theme = useMantineTheme()

  return (
    <Group
      noWrap
      position="center"
      miw={`calc(${theme.spacing.xl} * 3)`}
      m={`0 ${theme.spacing.xs}`}
      {...others}
    >
      {part.transformTo.map((value) => {
        const s = ParticipationStatusMap.get(value)!
        return (
          <ActionIconWithConfirm
            key={`${participateId}@${value}`}
            size={size}
            iconPath={s.iconPath}
            color={s.color}
            message={`确定要设为“${s.title}”吗？`}
            disabled={disabled}
            onClick={() => setParticipationStatus(participateId, value)}
          />
        )
      })}
    </Group>
  )
}
