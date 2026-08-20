import dayjs from 'dayjs'

export const getRegistrationStatusColor = (status: string): string => {
  switch (status) {
    case 'PENDING':
      return 'yellow'
    case 'APPROVED':
      return 'green'
    case 'REJECTED':
      return 'red'
    case 'CANCELLED':
      return 'gray'
    default:
      return 'blue'
  }
}

export const getRegistrationStatusLabel = (status: string): string => {
  switch (status) {
    case 'PENDING':
      return '待审核'
    case 'APPROVED':
      return '已通过'
    case 'REJECTED':
      return '已拒绝'
    case 'CANCELLED':
      return '已取消'
    default:
      return status
  }
}

export const getSponsorTypeLabel = (type: string): string => {
  switch (type) {
    case 'PLATINUM':
      return '白金赞助商'
    case 'GOLD':
      return '金牌赞助商'
    case 'SILVER':
      return '银牌赞助商'
    case 'BRONZE':
      return '铜牌赞助商'
    case 'PARTNER':
      return '合作伙伴'
    default:
      return type
  }
}

export const isRegistrationOpen = (startTime: string, endTime: string): boolean => {
  const now = dayjs()
  return now.isAfter(dayjs(startTime)) && now.isBefore(dayjs(endTime))
}

export const getRegistrationStatusText = (
  startTime: string,
  endTime: string
): { text: string; color: string } => {
  const now = dayjs()

  if (now.isBefore(dayjs(startTime))) {
    return {
      text: `报名将于 ${dayjs(startTime).format('YYYY-MM-DD HH:mm')} 开始`,
      color: 'blue'
    }
  }

  if (now.isAfter(dayjs(endTime))) {
    return {
      text: '报名已结束',
      color: 'gray'
    }
  }

  return {
    text: `报名进行中，截止时间：${dayjs(endTime).format('YYYY-MM-DD HH:mm')}`,
    color: 'green'
  }
}

export const formatDatetime = (datetime: string | null | undefined): string => {
  if (!datetime) return '-'
  return dayjs(datetime).format('YYYY-MM-DD HH:mm:ss')
}
