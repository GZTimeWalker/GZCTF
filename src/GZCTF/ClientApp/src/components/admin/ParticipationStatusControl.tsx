import { Group, GroupProps, MantineSpacing, useMantineTheme } from '@mantine/core'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import { ActionIconWithConfirm } from '@Components/ActionIconWithConfirm'
import { useParticipationStatusMap } from '@Utils/Shared'
import { ParticipationEditModel, ParticipationStatus } from '@Api'

interface ParticipationStatusControlProps extends GroupProps {
  disabled: boolean
  participateId: number
  size?: MantineSpacing
  status: ParticipationStatus
  setParticipation: (id: number, model: ParticipationEditModel) => Promise<void>
}

export const ParticipationStatusControl: FC<ParticipationStatusControlProps> = (props) => {
  const { disabled, participateId, status, setParticipation, size, ...others } = props
  const partStatusMap = useParticipationStatusMap()
  const part = partStatusMap.get(status)!
  const theme = useMantineTheme()

  const { t } = useTranslation()

  return (
    <Group wrap="nowrap" justify="center" mx="xs" miw={`calc(${theme.spacing.xl} * 2)`} {...others}>
      {part.transformTo.map((value) => {
        const s = partStatusMap.get(value)!
        return (
          <ActionIconWithConfirm
            key={`${participateId}@${value}`}
            size={size}
            iconPath={s.iconPath}
            color={s.color}
            message={t('admin.content.games.review.participation.update', {
              status: s.title,
            })}
            disabled={disabled}
            onClick={() => setParticipation(participateId, { status: value })}
          />
        )
      })}
    </Group>
  )
}
